using System.Security.Claims;
using System.Text;
using BarberBooking.Api.Contracts;
using BarberBooking.Api.Domain;
using BarberBooking.Api.Infrastructure;
using BarberBooking.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new()
{
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header usando o esquema Bearer."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Swagger - UI para testar endpoints
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarberBooking.Api v1"));

    // Redirecionar raiz (/) para Swagger
    app.MapGet("/", () => Results.Redirect("/swagger/index.html")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Users.AnyAsync(x => x.Role == UserRole.SuperAdmin))
    {
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        db.Users.Add(new User { Name = "Super Admin", Email = builder.Configuration["Seed:SuperAdminEmail"]!, PasswordHash = auth.Hash(builder.Configuration["Seed:SuperAdminPassword"]!), Role = UserRole.SuperAdmin, MustChangePassword = true });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var authApi = app.MapGroup("/api/auth");
authApi.MapPost("/login", async (LoginRequest input, AppDbContext db, AuthService auth) =>
{
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == input.Email.ToLower());
    if (user is null || !user.IsActive || !auth.Verify(input.Password, user.PasswordHash)) return Results.Unauthorized();
    return Results.Ok(new { accessToken = auth.Token(user), user = new { user.Id, user.Name, user.Email, role = user.Role.ToString(), user.TenantId, user.MustChangePassword } });
});
authApi.MapPost("/change-password", async (ChangePasswordRequest input, ClaimsPrincipal principal, AppDbContext db, AuthService auth) =>
{
    var user = await db.Users.FindAsync(principal.UserId());
    if (user is null || !auth.Verify(input.CurrentPassword, user.PasswordHash)) return Results.BadRequest(new { message = "Senha atual inválida." });
    if (input.NewPassword.Length < 8) return Results.BadRequest(new { message = "A senha deve ter pelo menos 8 caracteres." });
    user.PasswordHash = auth.Hash(input.NewPassword); user.MustChangePassword = false; await db.SaveChangesAsync(); return Results.NoContent();
}).RequireAuthorization();

var super = app.MapGroup("/api/super-admin").RequireAuthorization(p => p.RequireRole(UserRole.SuperAdmin.ToString()));
super.MapPost("/tenants", async (CreateTenantRequest input, AppDbContext db, AuthService auth) =>
{
    var slug = input.Slug.Trim().ToLowerInvariant();
    if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9-]{3,60}$")) return Results.BadRequest(new { message = "Slug inválido." });
    if (await db.Tenants.AnyAsync(x => x.Slug == slug) || await db.Users.AnyAsync(x => x.Email == input.AdminEmail.ToLower())) return Results.Conflict(new { message = "Slug ou e-mail já cadastrado." });
    var tempPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));
    var tenant = new Tenant { Name = input.Name, Slug = slug, Phone = input.Phone, Address = input.Address };
    var admin = new User { Tenant = tenant, Name = input.AdminName, Email = input.AdminEmail.ToLower(), PasswordHash = auth.Hash(tempPassword), Role = UserRole.TenantAdmin, MustChangePassword = true };
    db.AddRange(tenant, admin); await db.SaveChangesAsync();
    return Results.Created($"/api/public/{tenant.Slug}", new { tenant.Id, tenant.Name, tenant.Slug, adminEmail = admin.Email, temporaryPassword = tempPassword, adminPanelUrl = "/admin", publicUrl = $"/barbearias/{tenant.Slug}" });
});
super.MapGet("/tenants", async (AppDbContext db) => await db.Tenants.OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, x.Name, x.Slug, x.IsActive, x.CreatedAtUtc }).ToListAsync());

var admin = app.MapGroup("/api/admin").RequireAuthorization(p => p.RequireRole(UserRole.TenantAdmin.ToString()));
admin.MapPut("/settings", async (UpdateTenantRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    var tenant = await db.Tenants.FindAsync(user.TenantId()); if (tenant is null) return Results.NotFound();
    if (input.CancellationLimitMinutes < 0 || input.CancellationLimitMinutes > 10080) return Results.BadRequest(new { message = "Limite de cancelamento inválido." });
    tenant.Name = input.Name; tenant.Phone = input.Phone; tenant.Address = input.Address; tenant.TimeZoneId = input.TimeZoneId; tenant.CancellationLimitMinutes = input.CancellationLimitMinutes;
    await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapPost("/services", async (CreateServiceRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    if (input.Price < 0 || input.DurationMinutes is < 5 or > 480) return Results.BadRequest(new { message = "Preço ou duração inválidos." });
    var service = new Service { TenantId = user.TenantId(), Name = input.Name, Description = input.Description, Price = input.Price, DurationMinutes = input.DurationMinutes }; db.Services.Add(service); await db.SaveChangesAsync(); return Results.Created($"/api/admin/services/{service.Id}", service);
});
admin.MapGet("/services", async (ClaimsPrincipal user, AppDbContext db) => await db.Services.Where(x => x.TenantId == user.TenantId()).OrderBy(x => x.Name).ToListAsync());
admin.MapPost("/barbers", async (CreateBarberRequest input, ClaimsPrincipal user, AppDbContext db, AuthService auth) =>
{
    if (await db.Users.AnyAsync(x => x.Email == input.Email.ToLower())) return Results.Conflict(new { message = "E-mail já cadastrado." });
    var account = new User { TenantId = user.TenantId(), Name = input.Name, Email = input.Email.ToLower(), PasswordHash = auth.Hash(input.Password), Role = UserRole.Barber, MustChangePassword = true };
    var barber = new Barber { TenantId = user.TenantId(), User = account, Bio = input.Bio }; db.Barbers.Add(barber); await db.SaveChangesAsync(); return Results.Created($"/api/admin/barbers/{barber.Id}", new { barber.Id, account.Email });
});
admin.MapPut("/barbers/{barberId:guid}/services", async (Guid barberId, SetBarberServicesRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    var tenantId = user.TenantId(); var barber = await db.Barbers.SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == tenantId); if (barber is null) return Results.NotFound();
    var ids = input.ServiceIds.Distinct().ToArray(); if (await db.Services.CountAsync(x => x.TenantId == tenantId && ids.Contains(x.Id)) != ids.Length) return Results.BadRequest(new { message = "Há serviços inválidos." });
    db.BarberServices.RemoveRange(db.BarberServices.Where(x => x.BarberId == barberId)); db.BarberServices.AddRange(ids.Select(x => new BarberService { BarberId = barberId, ServiceId = x })); await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapPut("/barbers/{barberId:guid}/working-hours", async (Guid barberId, SetWorkingHoursRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    var barber = await db.Barbers.SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == user.TenantId()); if (barber is null) return Results.NotFound();
    var hours = input.Hours.ToArray(); if (hours.Any(x => x.End <= x.Start)) return Results.BadRequest(new { message = "Intervalos de trabalho inválidos." });
    db.WorkingHours.RemoveRange(db.WorkingHours.Where(x => x.BarberId == barberId)); db.WorkingHours.AddRange(hours.Select(x => new WorkingHour { BarberId = barberId, DayOfWeek = x.DayOfWeek, Start = x.Start, End = x.End })); await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapGet("/appointments", async (DateOnly? date, ClaimsPrincipal user, AppDbContext db) =>
{
    IQueryable<Appointment> query = db.Appointments.Include(x => x.Customer).Include(x => x.Barber!).ThenInclude(x => x.User!).Include(x => x.Service).Where(x => x.TenantId == user.TenantId());
    if (date is { } d) { var start = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); query = query.Where(x => x.StartAtUtc >= start && x.StartAtUtc < start.AddDays(1)); }
    return await query.OrderBy(x => x.StartAtUtc).ToListAsync();
});

var publicApi = app.MapGroup("/api/public/{slug}");
publicApi.MapGet("", async (string slug, AppDbContext db) =>
{
    var t = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); return t is null ? Results.NotFound() : Results.Ok(new { t.Name, t.Slug, t.Phone, t.Address, t.TimeZoneId, t.CancellationLimitMinutes });
});
publicApi.MapGet("/services", async (string slug, AppDbContext db) => await db.Services.Where(x => x.Tenant!.Slug == slug && x.IsActive).OrderBy(x => x.Name).ToListAsync());
publicApi.MapGet("/barbers", async (string slug, AppDbContext db) => await db.Barbers.Include(x => x.User).Where(x => x.Tenant!.Slug == slug && x.IsActive).Select(x => new { x.Id, name = x.User!.Name, x.Bio }).ToListAsync());
publicApi.MapGet("/availability", async (string slug, Guid barberId, Guid serviceId, DateOnly date, AppDbContext db, BookingService booking) =>
{
    var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); if (tenant is null) return Results.NotFound();
    var slots = await booking.AvailableSlots(tenant.Id, barberId, serviceId, date);
    return slots is null ? Results.BadRequest(new { message = "Barbeiro ou serviço inválido." }) : Results.Ok(slots);
});
publicApi.MapPost("/register", async (string slug, RegisterCustomerRequest input, AppDbContext db, AuthService auth) =>
{
    var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); if (tenant is null) return Results.NotFound();
    if (input.Password.Length < 8) return Results.BadRequest(new { message = "A senha deve ter pelo menos 8 caracteres." });
    if (await db.Users.AnyAsync(x => x.Email == input.Email.ToLower())) return Results.Conflict(new { message = "E-mail já cadastrado." });
    var customer = new User { TenantId = tenant.Id, Name = input.Name, Email = input.Email.ToLower(), PasswordHash = auth.Hash(input.Password), Role = UserRole.Customer }; db.Users.Add(customer); await db.SaveChangesAsync(); return Results.Ok(new { accessToken = auth.Token(customer) });
});

var customer = publicApi.MapGroup("").RequireAuthorization(p => p.RequireRole(UserRole.Customer.ToString()));
customer.MapPost("/appointments", async (string slug, CreateAppointmentRequest input, ClaimsPrincipal user, AppDbContext db, BookingService booking) =>
{
    var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); if (tenant is null || user.TenantId() != tenant.Id) return Results.Forbid();
    var result = await booking.Create(tenant.Id, user.UserId(), input.BarberId, input.ServiceId, input.StartAt, input.Notes); return result.Ok ? Results.Created($"/api/public/{slug}/appointments/{result.Appointment!.Id}", result.Appointment) : Results.BadRequest(new { message = result.Error });
});
customer.MapGet("/my-appointments", async (string slug, ClaimsPrincipal user, AppDbContext db) => await db.Appointments.Include(x => x.Barber!).ThenInclude(x => x.User!).Include(x => x.Service).Where(x => x.CustomerId == user.UserId() && x.Tenant!.Slug == slug).OrderByDescending(x => x.StartAtUtc).ToListAsync());
customer.MapPost("/appointments/{id:guid}/cancel", async (string slug, Guid id, ClaimsPrincipal user, AppDbContext db) =>
{
    var appointment = await db.Appointments.Include(x => x.Tenant).SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == user.UserId() && x.Tenant!.Slug == slug); if (appointment is null) return Results.NotFound();
    if (appointment.Status != AppointmentStatus.Confirmed || appointment.StartAtUtc <= DateTime.UtcNow.AddMinutes(appointment.Tenant!.CancellationLimitMinutes)) return Results.BadRequest(new { message = "O prazo de cancelamento expirou." });
    appointment.Status = AppointmentStatus.Cancelled; await db.SaveChangesAsync(); return Results.NoContent();
});
customer.MapPost("/appointments/{id:guid}/reschedule", async (string slug, Guid id, RescheduleAppointmentRequest input, ClaimsPrincipal user, AppDbContext db, BookingService booking) =>
{
    var appointment = await db.Appointments.Include(x => x.Tenant).SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == user.UserId() && x.Tenant!.Slug == slug);
    if (appointment is null) return Results.NotFound();
    if (appointment.Status != AppointmentStatus.Confirmed || appointment.StartAtUtc <= DateTime.UtcNow.AddMinutes(appointment.Tenant!.CancellationLimitMinutes)) return Results.BadRequest(new { message = "O prazo para remarcar expirou." });
    var result = await booking.Create(appointment.TenantId, user.UserId(), input.BarberId, appointment.ServiceId, input.StartAt, appointment.Notes);
    if (!result.Ok) return Results.BadRequest(new { message = result.Error });
    appointment.Status = AppointmentStatus.Cancelled; await db.SaveChangesAsync(); return Results.Ok(result.Appointment);
});

app.Run();

public static class ClaimsExtensions
{
    public static Guid UserId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);
    public static Guid TenantId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("tenant_id")!);
}
