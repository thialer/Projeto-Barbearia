namespace BarberBooking.Api.Contracts;

public record LoginRequest(string Email, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record CreateTenantRequest(string Name, string Slug, string AdminName, string AdminEmail, string? Phone, string? Address);
public record RegisterCustomerRequest(string Name, string Email, string Password);
public record CreateBarberRequest(string Name, string Email, string Password, string? Bio);
public record CreateServiceRequest(string Name, string? Description, decimal Price, int DurationMinutes);
public record UpdateTenantRequest(string Name, string? Phone, string? Address, string TimeZoneId, int CancellationLimitMinutes);
public record SetWorkingHoursRequest(IEnumerable<WorkingHourRequest> Hours);
public record WorkingHourRequest(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End);
public record SetBarberServicesRequest(IEnumerable<Guid> ServiceIds);
public record CreateAppointmentRequest(Guid BarberId, Guid ServiceId, DateTime StartAt, string? Notes);
public record RescheduleAppointmentRequest(Guid BarberId, DateTime StartAt);
