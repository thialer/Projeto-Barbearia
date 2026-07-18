using BarberBooking.Api.Domain;
using BarberBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace BarberBooking.Api.Services;

public sealed class BookingService(AppDbContext db, SchedulingCache schedulingCache, BookingMetrics metrics)
{
    public async Task<IReadOnlyList<DateTime>?> AvailableSlots(Guid tenantId, Guid barberId, IReadOnlyCollection<Guid> serviceIds, DateOnly date)
    {
        var services = await GetBookableServices(tenantId, barberId, serviceIds);
        var barber = await db.Barbers.Include(x => x.WorkingHours)
            .SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == tenantId && x.IsActive);
        var tenant = await db.Tenants.FindAsync(tenantId);
        if (services is null || barber is null || tenant is null) return null;

        var cachedSlots = await schedulingCache.GetAvailability(tenantId, barberId, serviceIds, date);
        if (cachedSlots is not null) return cachedSlots;

        var totalDuration = services.Sum(x => x.DurationMinutes);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZoneId);
        var dayStartUtc = ToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
        var dayEndUtc = ToUtc(date.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        var appointments = await db.Appointments
            .Where(x => x.BarberId == barberId && x.Status != AppointmentStatus.Cancelled && x.StartAtUtc < dayEndUtc && x.EndAtUtc > dayStartUtc)
            .ToListAsync();

        var slots = new List<DateTime>();
        foreach (var work in barber.WorkingHours.Where(x => x.DayOfWeek == date.DayOfWeek))
        {
            var workStart = date.ToDateTime(work.Start);
            var workEnd = date.ToDateTime(work.End);
            for (var slot = workStart; slot.AddMinutes(totalDuration) <= workEnd; slot = slot.AddMinutes(tenant.SlotIntervalMinutes))
            {
                if (zone.IsInvalidTime(slot) || zone.IsAmbiguousTime(slot)) continue;
                if (slot <= TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone)) continue;

                var startUtc = ToUtc(slot, zone);
                var endUtc = startUtc.AddMinutes(totalDuration);
                if (!appointments.Any(x => x.StartAtUtc < endUtc && x.EndAtUtc > startUtc)) slots.Add(slot);
            }
        }

        var availableSlots = slots.Distinct().OrderBy(x => x).ToArray();
        await schedulingCache.SetAvailability(tenantId, barberId, serviceIds, date, availableSlots);
        return availableSlots;
    }

    public async Task<(bool Ok, string? Error, Appointment? Appointment, bool Conflict)> Create(
        Guid tenantId, Guid customerId, Guid barberId, IReadOnlyCollection<Guid> serviceIds, DateTime startAt, string? notes)
    {
        var services = await GetBookableServices(tenantId, barberId, serviceIds);
        var barber = await db.Barbers.Include(x => x.WorkingHours)
            .SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == tenantId && x.IsActive);
        var tenant = await db.Tenants.FindAsync(tenantId);
        if (services is null || barber is null || tenant is null)
            return (false, "Barbeiro, serviço ou barbearia inválidos.", null, false);

        var totalDuration = services.Sum(x => x.DurationMinutes);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZoneId);
        var localRequestedStart = DateTime.SpecifyKind(startAt, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(localRequestedStart) || zone.IsAmbiguousTime(localRequestedStart))
            return (false, "Horário inválido para o fuso da barbearia.", null, false);

        var utc = ToUtc(localRequestedStart, zone);
        var end = utc.AddMinutes(totalDuration);
        if (utc <= DateTime.UtcNow) return (false, "O horário precisa estar no futuro.", null, false);

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(end, zone);
        var work = barber.WorkingHours.SingleOrDefault(x =>
            x.DayOfWeek == localStart.DayOfWeek &&
            localStart.Date == localEnd.Date &&
            localStart.TimeOfDay >= x.Start.ToTimeSpan() &&
            localEnd.TimeOfDay <= x.End.ToTimeSpan());
        if (work is null) return (false, "Horário fora do expediente do barbeiro.", null, false);

        if ((localStart.TimeOfDay - work.Start.ToTimeSpan()).TotalMinutes % tenant.SlotIntervalMinutes != 0)
            return (false, "O horário precisa respeitar o intervalo de agenda da barbearia.", null, false);

        var conflict = await db.Appointments.AnyAsync(x =>
            x.BarberId == barberId && x.Status != AppointmentStatus.Cancelled && x.StartAtUtc < end && x.EndAtUtc > utc);
        if (conflict)
        {
            metrics.AppointmentConflict();
            return (false, "Este horário acabou de ser reservado.", null, true);
        }

        var appointment = new Appointment
        {
            TenantId = tenantId,
            CustomerId = customerId,
            BarberId = barberId,
            ServiceId = services[0].Id,
            StartAtUtc = utc,
            EndAtUtc = end,
            Notes = notes,
            AppointmentServices = services.Select(x => new AppointmentService { ServiceId = x.Id }).ToList()
        };
        db.Appointments.Add(appointment);
        db.OutboxMessages.Add(new OutboxMessage
        {
            TenantId = tenantId,
            Type = OutboxMessageType.AppointmentCreated,
            Payload = JsonSerializer.Serialize(new { appointment.Id, appointment.CustomerId, appointment.BarberId, appointment.StartAtUtc, appointment.EndAtUtc })
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            metrics.AppointmentConflict();
            return (false, "Este horário acabou de ser reservado.", null, true);
        }

        metrics.AppointmentCreated();
        await schedulingCache.InvalidateTenant(tenantId);
        return (true, null, appointment, false);
    }

    private async Task<List<Service>?> GetBookableServices(Guid tenantId, Guid barberId, IReadOnlyCollection<Guid> serviceIds)
    {
        var ids = serviceIds.Distinct().ToArray();
        if (ids.Length == 0 || ids.Length != serviceIds.Count) return null;

        var services = await db.Services.Where(x => ids.Contains(x.Id) && x.TenantId == tenantId && x.IsActive).ToListAsync();
        if (services.Count != ids.Length) return null;

        var offeredServiceIds = await db.BarberServices.Where(x => x.BarberId == barberId).Select(x => x.ServiceId).ToListAsync();
        return ids.All(id => offeredServiceIds.Contains(id)) ? services.OrderBy(x => Array.IndexOf(ids, x.Id)).ToList() : null;
    }

    private static DateTime ToUtc(DateTime localTime, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), zone);
}
