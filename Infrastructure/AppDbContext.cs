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
    public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Service>().Property(x => x.Price).HasPrecision(10, 2);
        b.Entity<BarberService>().HasKey(x => new { x.BarberId, x.ServiceId });
        b.Entity<BarberService>().HasOne(x => x.Barber).WithMany(x => x.BarberServices).HasForeignKey(x => x.BarberId);
        b.Entity<BarberService>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId);
        b.Entity<Barber>().HasOne(x => x.User).WithOne().HasForeignKey<Barber>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasIndex(x => new { x.BarberId, x.StartAtUtc });
        b.Entity<Appointment>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(x => x.Barber).WithMany().HasForeignKey(x => x.BarberId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Appointment>().HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
