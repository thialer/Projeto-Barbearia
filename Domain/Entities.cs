namespace BarberBooking.Api.Domain;

public enum UserRole { SuperAdmin, TenantAdmin, Barber, Customer }
public enum AppointmentStatus { Confirmed, Cancelled, Completed, NoShow }

public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
    public int CancellationLimitMinutes { get; set; } = 120;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<Service> Services { get; set; } = [];
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Barber
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public List<BarberService> BarberServices { get; set; } = [];
    public List<WorkingHour> WorkingHours { get; set; } = [];
}

public sealed class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BarberService { public Guid BarberId { get; set; } public Barber? Barber { get; set; } public Guid ServiceId { get; set; } public Service? Service { get; set; } }
public sealed class WorkingHour
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BarberId { get; set; }
    public Barber? Barber { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
}

public sealed class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid CustomerId { get; set; }
    public User? Customer { get; set; }
    public Guid BarberId { get; set; }
    public Barber? Barber { get; set; }
    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
