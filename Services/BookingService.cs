using BarberBooking.Api.Domain;
using BarberBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Api.Services;

public sealed class BookingService(AppDbContext db)
{
    public async Task<IReadOnlyList<DateTime>?> AvailableSlots(Guid tenantId, Guid barberId, Guid serviceId, DateOnly date)
    {
        var service = await db.Services.SingleOrDefaultAsync(x => x.Id == serviceId && x.TenantId == tenantId && x.IsActive);
        var barber = await db.Barbers.Include(x => x.WorkingHours).Include(x => x.BarberServices).SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == tenantId && x.IsActive);
        var tenant = await db.Tenants.FindAsync(tenantId);
        if (service is null || barber is null || tenant is null || !barber.BarberServices.Any(x => x.ServiceId == serviceId)) return null;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZoneId);
        var dayHours = barber.WorkingHours.Where(x => x.DayOfWeek == date.DayOfWeek).ToList();
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
        var dayEndUtc = dayStartUtc.AddDays(1);
        var appointments = await db.Appointments.Where(x => x.BarberId == barberId && x.Status == AppointmentStatus.Confirmed && x.StartAtUtc < dayEndUtc && x.EndAtUtc > dayStartUtc).ToListAsync();
        var slots = new List<DateTime>();
        foreach (var work in dayHours)
        for (var slot = date.ToDateTime(work.Start); slot.AddMinutes(service.DurationMinutes).TimeOfDay <= work.End.ToTimeSpan(); slot = slot.AddMinutes(15))
        {
            if (slot <= TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone)) continue;
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(slot, zone); var endUtc = startUtc.AddMinutes(service.DurationMinutes);
            if (!appointments.Any(x => x.StartAtUtc < endUtc && x.EndAtUtc > startUtc)) slots.Add(slot);
        }
        return slots;
    }

    public async Task<(bool Ok, string? Error, Appointment? Appointment)> Create(Guid tenantId, Guid customerId, Guid barberId, Guid serviceId, DateTime startAt, string? notes)
    {
        var service = await db.Services.SingleOrDefaultAsync(x => x.Id == serviceId && x.TenantId == tenantId && x.IsActive);
        var barber = await db.Barbers.Include(x => x.WorkingHours).Include(x => x.BarberServices).SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == tenantId && x.IsActive);
        if (service is null || barber is null || !barber.BarberServices.Any(x => x.ServiceId == serviceId)) return (false, "Barbeiro ou serviço inválido.", null);
        var tenant = await db.Tenants.FindAsync(tenantId);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(tenant!.TimeZoneId);
        var utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startAt, DateTimeKind.Unspecified), zone);
        var end = utc.AddMinutes(service.DurationMinutes);
        if (utc <= DateTime.UtcNow) return (false, "O horário precisa estar no futuro.", null);
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(end, zone);
        var work = barber.WorkingHours.Any(x => x.DayOfWeek == localStart.DayOfWeek && localStart.TimeOfDay >= x.Start.ToTimeSpan() && localEnd.TimeOfDay <= x.End.ToTimeSpan());
        if (!work) return (false, "Horário fora do expediente do barbeiro.", null);
        var conflict = await db.Appointments.AnyAsync(x => x.BarberId == barberId && x.Status == AppointmentStatus.Confirmed && x.StartAtUtc < end && x.EndAtUtc > utc);
        if (conflict) return (false, "Este horário acabou de ser reservado.", null);
        var appointment = new Appointment { TenantId = tenantId, CustomerId = customerId, BarberId = barberId, ServiceId = serviceId, StartAtUtc = utc, EndAtUtc = end, Notes = notes };
        db.Appointments.Add(appointment); await db.SaveChangesAsync(); return (true, null, appointment);
    }
}
