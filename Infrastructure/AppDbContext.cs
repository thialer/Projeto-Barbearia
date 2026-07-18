using BarberBooking.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BarberBooking.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BarberService> BarberServices => Set<BarberService>();
    public DbSet<AppointmentService> AppointmentServices => Set<AppointmentService>();
    public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Service>().Property(x => x.Price).HasPrecision(10, 2);
        b.Entity<BarberService>().HasKey(x => new { x.BarberId, x.ServiceId });
        b.Entity<BarberService>().HasOne(x => x.Barber).WithMany(x => x.BarberServices).HasForeignKey(x => x.BarberId);
        b.Entity<BarberService>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId);
        b.Entity<AppointmentService>().HasKey(x => new { x.AppointmentId, x.ServiceId });
        b.Entity<AppointmentService>().HasOne(x => x.Appointment).WithMany(x => x.AppointmentServices).HasForeignKey(x => x.AppointmentId);
        b.Entity<AppointmentService>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Barber>().HasOne(x => x.User).WithOne().HasForeignKey<Barber>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasIndex(x => new { x.BarberId, x.StartAtUtc });
        b.Entity<Appointment>().HasIndex(x => new { x.BarberId, x.Status, x.StartAtUtc });
        b.Entity<Appointment>().HasIndex(x => new { x.TenantId, x.StartAtUtc });
        b.Entity<WorkingHour>().HasIndex(x => new { x.BarberId, x.DayOfWeek });
        b.Entity<Service>().HasIndex(x => new { x.TenantId, x.IsActive });
        b.Entity<IdempotencyRequest>().HasIndex(x => new { x.TenantId, x.CustomerId, x.Key }).IsUnique();
        b.Entity<OutboxMessage>().HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });
        b.Entity<Tenant>().Property(x => x.SlotIntervalMinutes).HasDefaultValue(30);
        b.Entity<Appointment>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(x => x.Barber).WithMany().HasForeignKey(x => x.BarberId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
