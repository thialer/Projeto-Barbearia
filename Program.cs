using System.Security.Claims;
using System.Text;
using System.Globalization;
using System.Threading.RateLimiting;
using System.Security.Cryptography;
using BarberBooking.Api.Contracts;
using BarberBooking.Api.Domain;
using BarberBooking.Api.Infrastructure;
using BarberBooking.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<SchedulingCache>();
builder.Services.AddSingleton<BookingMetrics>();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnection))
    builder.Services.AddDistributedMemoryCache();
else
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("booking", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Retornar configurações do tenant para o painel admin (rota declarada mais abaixo dentro do bloco 'admin')
builder.Services.AddOutputCache(options => options.AddPolicy("catalog", policy =>
    policy.Expire(TimeSpan.FromMinutes(5)).SetVaryByRouteValue("slug")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new()
    {
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    o.Events = new JwtBearerEvents
    {
    OnTokenValidated = async context =>
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
        {
            context.Fail("Invalid token.");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.Users.Include(x => x.Tenant).SingleOrDefaultAsync(x => x.Id == id);
        if (user is null || !user.IsActive || (user.TenantId is not null && (user.Tenant is null || !user.Tenant.IsActive)))
            context.Fail("Inactive account.");
    }
    };
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
            "http://localhost:3000",
            "http://127.0.0.1:3000",
            "https://localhost:3000",
            "http://localhost:3001",
            "http://127.0.0.1:3001",
            "https://localhost:3001"
        ).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
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

app.UseCors("Frontend");

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Users.AnyAsync(x => x.Role == UserRole.SuperAdmin))
    {
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        db.Users.Add(new User { Name = "Super Admin", Email = builder.Configuration["Seed:SuperAdminEmail"]!, PasswordHash = auth.Hash(builder.Configuration["Seed:SuperAdminPassword"]!), Role = UserRole.SuperAdmin, MustChangePassword = true });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "healthy" })
        : Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable));

var authApi = app.MapGroup("/api/auth").RequireRateLimiting("auth");
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
    if (!EmailValidator.IsValid(input.AdminEmail)) return Results.BadRequest(new { message = "E-mail do administrador inv\u00e1lido." });
    var slug = input.Slug.Trim().ToLowerInvariant();
    if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9-]{3,60}$")) return Results.BadRequest(new { message = "Slug inválido." });
    if (await db.Tenants.AnyAsync(x => x.Slug == slug) || await db.Users.AnyAsync(x => x.Email == input.AdminEmail.ToLower())) return Results.Conflict(new { message = "Slug ou e-mail já cadastrado." });
    const string tempPassword = "Barbearia1234&";
    var tenant = new Tenant { Name = input.Name, Slug = slug, Phone = input.Phone, Address = input.Address };
    var admin = new User { Tenant = tenant, Name = input.AdminName, Email = input.AdminEmail.ToLower(), PasswordHash = auth.Hash(tempPassword), Role = UserRole.TenantAdmin, MustChangePassword = true };
    db.AddRange(tenant, admin); await db.SaveChangesAsync();
    return Results.Created($"/api/public/{tenant.Slug}", new { tenant.Id, tenant.Name, tenant.Slug, adminEmail = admin.Email, temporaryPassword = tempPassword, adminPanelUrl = "/admin", publicUrl = $"/barbearias/{tenant.Slug}" });
});
super.MapGet("/tenants", async (AppDbContext db) => await db.Tenants.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, x.Name, x.Slug, x.IsActive, x.CreatedAtUtc }).ToListAsync());
super.MapDelete("/tenants/{tenantId:guid}", async (Guid tenantId, AppDbContext db) =>
{
    var tenant = await db.Tenants.FindAsync(tenantId);
    if (tenant is null) return Results.NotFound();
    if (!tenant.IsActive) return Results.NoContent();

    tenant.IsActive = false;
    await db.Users.Where(x => x.TenantId == tenantId).ExecuteUpdateAsync(x => x.SetProperty(user => user.IsActive, false));
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Duplicate MapGet("/settings") removed to avoid compilation error (was declared before the admin group)
var admin = app.MapGroup("/api/admin").RequireAuthorization(p => p.RequireRole(UserRole.TenantAdmin.ToString()));
admin.MapGet("/settings", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var tenant = await db.Tenants.FindAsync(user.TenantId());
    if (tenant is null) return Results.NotFound();
    return Results.Ok(new { tenant.Name, tenant.Phone, tenant.Address, tenant.TimeZoneId, tenant.CancellationLimitMinutes, tenant.SlotIntervalMinutes });
});
admin.MapPut("/settings", async (UpdateTenantRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    var tenant = await db.Tenants.FindAsync(user.TenantId()); if (tenant is null) return Results.NotFound();
    if (input.CancellationLimitMinutes < 0 || input.CancellationLimitMinutes > 10080) return Results.BadRequest(new { message = "Limite de cancelamento inválido." });
    if (input.SlotIntervalMinutes is { } slotInterval && (slotInterval < 5 || slotInterval > 60 || slotInterval % 5 != 0)) return Results.BadRequest(new { message = "Intervalo de agenda inválido." });
    try { _ = TimeZoneInfo.FindSystemTimeZoneById(input.TimeZoneId); }
    catch (TimeZoneNotFoundException) { return Results.BadRequest(new { message = "Fuso horário inválido." }); }
    catch (InvalidTimeZoneException) { return Results.BadRequest(new { message = "Fuso horário inválido." }); }
    tenant.Name = input.Name; tenant.Phone = input.Phone; tenant.Address = input.Address; tenant.TimeZoneId = input.TimeZoneId; tenant.CancellationLimitMinutes = input.CancellationLimitMinutes;
    if (input.SlotIntervalMinutes is { } validSlotInterval) tenant.SlotIntervalMinutes = validSlotInterval;
    await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapPost("/services", async (CreateServiceRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    if (input.Price < 0 || input.DurationMinutes is < 5 or > 480) return Results.BadRequest(new { message = "Preço ou duração inválidos." });
    var service = new Service { TenantId = user.TenantId(), Name = input.Name, Description = input.Description, Price = input.Price, DurationMinutes = input.DurationMinutes };
    db.Services.Add(service);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/services/{service.Id}", new { service.Id, service.Name, service.Description, service.Price, service.DurationMinutes, service.IsActive });
});
admin.MapGet("/services", async (ClaimsPrincipal user, AppDbContext db) => await db.Services.AsNoTracking()
    .Where(x => x.TenantId == user.TenantId())
    .OrderBy(x => x.Name)
    .Select(x => new { x.Id, x.Name, x.Description, x.Price, x.DurationMinutes, x.IsActive })
    .ToListAsync());
admin.MapPost("/barbers", async (CreateBarberRequest input, ClaimsPrincipal user, AppDbContext db, AuthService auth) =>
{
    if (!EmailValidator.IsValid(input.Email)) return Results.BadRequest(new { message = "E-mail inv\u00e1lido." });
    if (await db.Users.AnyAsync(x => x.Email == input.Email.ToLower())) return Results.Conflict(new { message = "E-mail já cadastrado." });
    var account = new User { TenantId = user.TenantId(), Name = input.Name, Email = input.Email.ToLower(), PasswordHash = auth.Hash(input.Password), Role = UserRole.Barber, MustChangePassword = true };
    var barber = new Barber { TenantId = user.TenantId(), User = account, Bio = input.Bio }; db.Barbers.Add(barber); await db.SaveChangesAsync(); return Results.Created($"/api/admin/barbers/{barber.Id}", new { barber.Id, account.Email });
});
admin.MapGet("/barbers", async (ClaimsPrincipal user, AppDbContext db) => await db.Barbers.AsNoTracking()
    .Include(x => x.User)
    .Where(x => x.TenantId == user.TenantId() && x.IsActive)
    .OrderBy(x => x.User!.Name)
    .Select(x => new { x.Id, name = x.User!.Name, email = x.User!.Email, x.Bio, x.IsActive })
    .ToListAsync());
admin.MapPut("/barbers/{barberId:guid}/services", async (Guid barberId, SetBarberServicesRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    var tenantId = user.TenantId(); var barber = await db.Barbers.SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == tenantId); if (barber is null) return Results.NotFound();
    var ids = input.ServiceIds.Distinct().ToArray(); if (await db.Services.CountAsync(x => x.TenantId == tenantId && ids.Contains(x.Id)) != ids.Length) return Results.BadRequest(new { message = "Há serviços inválidos." });
    db.BarberServices.RemoveRange(db.BarberServices.Where(x => x.BarberId == barberId)); db.BarberServices.AddRange(ids.Select(x => new BarberService { BarberId = barberId, ServiceId = x })); await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapPut("/barbers/{barberId:guid}/working-hours", async (Guid barberId, SetWorkingHoursRequest input, ClaimsPrincipal user, AppDbContext db) =>
{
    var barber = await db.Barbers.SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == user.TenantId()); if (barber is null) return Results.NotFound();
    var hours = input.Hours.ToArray();
    var hasOverlappingHours = hours.GroupBy(x => x.DayOfWeek).Any(group =>
    {
        var ordered = group.OrderBy(x => x.Start).ToArray();
        return ordered.Zip(ordered.Skip(1), (current, next) => next.Start < current.End).Any(x => x);
    });
    if (hours.Any(x => !Enum.IsDefined(x.DayOfWeek) || x.End <= x.Start) || hasOverlappingHours) return Results.BadRequest(new { message = "Intervalos de trabalho inválidos ou sobrepostos." });
    db.WorkingHours.RemoveRange(db.WorkingHours.Where(x => x.BarberId == barberId)); db.WorkingHours.AddRange(hours.Select(x => new WorkingHour { BarberId = barberId, DayOfWeek = x.DayOfWeek, Start = x.Start, End = x.End })); await db.SaveChangesAsync(); return Results.NoContent();
});
admin.MapDelete("/barbers/{barberId:guid}", async (Guid barberId, ClaimsPrincipal user, AppDbContext db) =>
{
    var barber = await db.Barbers.SingleOrDefaultAsync(x => x.Id == barberId && x.TenantId == user.TenantId());
    if (barber is null) return Results.NotFound();
    if (!barber.IsActive) return Results.NoContent();

    barber.IsActive = false;
    var user_entity = await db.Users.FindAsync(barber.UserId);
    if (user_entity is not null) user_entity.IsActive = false;
    await db.SaveChangesAsync();
    return Results.NoContent();
});
admin.MapGet("/appointments", async (DateOnly? date, int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    IQueryable<Appointment> query = db.Appointments.AsNoTracking().Where(x => x.TenantId == user.TenantId());
    if (date is { } d)
    {
        var tenant = await db.Tenants.FindAsync(user.TenantId());
        if (tenant is not null)
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZoneId);
            var start = TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), zone);
            var end = TimeZoneInfo.ConvertTimeToUtc(d.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
            query = query.Where(x => x.StartAtUtc >= start && x.StartAtUtc < end);
        }
    }
    var appointmentQuery = query.OrderBy(x => x.StartAtUtc).Select(x => new
    {
        x.Id,
        x.CustomerId,
        customerName = x.Customer!.Name,
        x.BarberId,
        barberName = x.Barber!.User!.Name,
        x.ServiceId,
        serviceName = x.Service!.Name,
        services = x.AppointmentServices.Select(item => new { item.ServiceId, name = item.Service!.Name, item.Service.Price, item.Service.DurationMinutes }),
        x.StartAtUtc,
        x.EndAtUtc,
        x.Status,
        x.Notes
    });
    if (page is null && pageSize is null) return Results.Ok(await appointmentQuery.ToListAsync());

    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 100);
    var total = await query.CountAsync();
    var items = await appointmentQuery.Skip((currentPage - 1) * size).Take(size).ToListAsync();
    return Results.Ok(new { items, page = currentPage, pageSize = size, total });
});

var publicApi = app.MapGroup("/api/public/{slug}");
publicApi.MapGet("", async (string slug, AppDbContext db) =>
{
    var t = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); return t is null ? Results.NotFound() : Results.Ok(new { t.Name, t.Slug, t.Phone, t.Address, t.TimeZoneId, t.SlotIntervalMinutes, t.CancellationLimitMinutes });
}).CacheOutput("catalog");
publicApi.MapGet("/services", async (string slug, AppDbContext db) => await db.Services.AsNoTracking()
    .Where(x => x.Tenant!.Slug == slug && x.IsActive)
    .OrderBy(x => x.Name)
    .Select(x => new { x.Id, x.Name, x.Description, x.Price, x.DurationMinutes })
    .ToListAsync()).CacheOutput("catalog");
publicApi.MapGet("/barbers", async (string slug, AppDbContext db) => await db.Barbers.AsNoTracking().Include(x => x.User).Where(x => x.Tenant!.Slug == slug && x.IsActive).Select(x => new { x.Id, name = x.User!.Name, x.Bio }).ToListAsync()).CacheOutput("catalog");
publicApi.MapGet("/availability", async (string slug, Guid barberId, Guid? serviceId, Guid[]? serviceIds, string date, AppDbContext db, BookingService booking) =>
{
    if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var bookingDate))
        return Results.BadRequest(new { message = "Data invÃ¡lida. Use o formato AAAA-MM-DD." });
    var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); if (tenant is null) return Results.NotFound();
    var requestedServiceIds = serviceIds is { Length: > 0 } ? serviceIds : (serviceId is { } legacyServiceId ? [legacyServiceId] : []);
    var slots = await booking.AvailableSlots(tenant.Id, barberId, requestedServiceIds, bookingDate);
    return slots is null ? Results.BadRequest(new { message = "Barbeiro ou serviços inválidos." }) : Results.Ok(slots);
}).RequireRateLimiting("public");
publicApi.MapPost("/register", async (string slug, RegisterCustomerRequest input, AppDbContext db, AuthService auth) =>
{
    if (!EmailValidator.IsValid(input.Email)) return Results.BadRequest(new { message = "E-mail inv\u00e1lido." });
    var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); if (tenant is null) return Results.NotFound();
    if (input.Password.Length < 8) return Results.BadRequest(new { message = "A senha deve ter pelo menos 8 caracteres." });
    if (await db.Users.AnyAsync(x => x.Email == input.Email.ToLower())) return Results.Conflict(new { message = "E-mail já cadastrado." });
    var customer = new User { TenantId = tenant.Id, Name = input.Name, Email = input.Email.ToLower(), PasswordHash = auth.Hash(input.Password), Role = UserRole.Customer }; db.Users.Add(customer); await db.SaveChangesAsync(); return Results.Ok(new { accessToken = auth.Token(customer) });
}).RequireRateLimiting("public");

var customer = publicApi.MapGroup("").RequireAuthorization(p => p.RequireRole(UserRole.Customer.ToString()));
customer.MapPost("/appointments", async (string slug, CreateAppointmentRequest input, ClaimsPrincipal user, HttpRequest request, AppDbContext db, BookingService booking) =>
{
    var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Slug == slug && x.IsActive); if (tenant is null || user.TenantId() != tenant.Id) return Results.Forbid();
    var requestedServiceIds = input.ServiceIds?.ToArray() ?? (input.ServiceId is { } legacyServiceId ? [legacyServiceId] : []);
    var idempotencyKey = request.Headers["Idempotency-Key"].ToString().Trim();
    if (idempotencyKey.Length > 100) return Results.BadRequest(new { message = "Chave de idempotência inválida." });
    var requestHash = IdempotencyHash.For(input, requestedServiceIds);
    if (idempotencyKey.Length > 0)
    {
        var previous = await db.IdempotencyRequests.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenant.Id && x.CustomerId == user.UserId() && x.Key == idempotencyKey);
        if (previous is not null)
        {
            if (previous.RequestHash != requestHash) return Results.Conflict(new { message = "A chave de idempotência já foi usada para outra solicitação." });
            return Results.Ok(new { appointmentId = previous.AppointmentId, replayed = true });
        }
    }
    var result = await booking.Create(tenant.Id, user.UserId(), input.BarberId, requestedServiceIds, input.StartAt, input.Notes);
    if (!result.Ok) return result.Conflict ? Results.Conflict(new { message = result.Error }) : Results.BadRequest(new { message = result.Error });

    var appointment = result.Appointment!;
    if (idempotencyKey.Length > 0)
    {
        db.IdempotencyRequests.Add(new IdempotencyRequest { TenantId = tenant.Id, CustomerId = user.UserId(), Key = idempotencyKey, RequestHash = requestHash, AppointmentId = appointment.Id });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Results.Conflict(new { message = "A chave de idempotência acabou de ser usada. Consulte o agendamento antes de repetir a solicitação." });
        }
    }
    return Results.Created($"/api/public/{slug}/appointments/{appointment.Id}", new
    {
        appointment.Id,
        appointment.BarberId,
        appointment.ServiceId,
        serviceIds = appointment.AppointmentServices.Select(item => item.ServiceId),
        appointment.StartAtUtc,
        appointment.EndAtUtc,
        status = appointment.Status.ToString(),
        appointment.Notes
    });
}).RequireRateLimiting("booking");
customer.MapGet("/my-appointments", async (string slug, int? page, int? pageSize, ClaimsPrincipal user, AppDbContext db) =>
{
    var query = db.Appointments.AsNoTracking().Where(x => x.CustomerId == user.UserId() && x.Tenant!.Slug == slug);
    var appointmentQuery = query.OrderByDescending(x => x.StartAtUtc).Select(x => new
    {
        x.Id,
        x.BarberId,
        barberName = x.Barber!.User!.Name,
        x.ServiceId,
        serviceName = x.Service!.Name,
        services = x.AppointmentServices.Select(item => new { item.ServiceId, name = item.Service!.Name, item.Service.Price, item.Service.DurationMinutes }),
        x.StartAtUtc,
        x.EndAtUtc,
        x.Status,
        x.Notes
    });
    if (page is null && pageSize is null) return Results.Ok(await appointmentQuery.ToListAsync());

    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 100);
    var total = await query.CountAsync();
    var items = await appointmentQuery.Skip((currentPage - 1) * size).Take(size).ToListAsync();
    return Results.Ok(new { items, page = currentPage, pageSize = size, total });
});
customer.MapPost("/appointments/{id:guid}/cancel", async (string slug, Guid id, ClaimsPrincipal user, AppDbContext db, SchedulingCache schedulingCache, BookingMetrics metrics) =>
{
    var appointment = await db.Appointments.Include(x => x.Tenant).SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == user.UserId() && x.Tenant!.Slug == slug); if (appointment is null) return Results.NotFound();
    if (appointment.Status != AppointmentStatus.Confirmed || appointment.StartAtUtc <= DateTime.UtcNow.AddMinutes(appointment.Tenant!.CancellationLimitMinutes)) return Results.BadRequest(new { message = "O prazo de cancelamento expirou." });
    appointment.Status = AppointmentStatus.Cancelled;
    db.OutboxMessages.Add(new OutboxMessage { TenantId = appointment.TenantId, Type = OutboxMessageType.AppointmentCancelled, Payload = System.Text.Json.JsonSerializer.Serialize(new { appointment.Id, appointment.CustomerId, appointment.BarberId, appointment.StartAtUtc }) });
    await db.SaveChangesAsync();
    metrics.AppointmentCancelled();
    await schedulingCache.InvalidateTenant(appointment.TenantId);
    return Results.NoContent();
});
customer.MapPost("/appointments/{id:guid}/reschedule", async (string slug, Guid id, RescheduleAppointmentRequest input, ClaimsPrincipal user, AppDbContext db, BookingService booking, SchedulingCache schedulingCache, BookingMetrics metrics) =>
{
    var appointment = await db.Appointments.Include(x => x.Tenant).Include(x => x.AppointmentServices).SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == user.UserId() && x.Tenant!.Slug == slug);
    if (appointment is null) return Results.NotFound();
    if (appointment.Status != AppointmentStatus.Confirmed || appointment.StartAtUtc <= DateTime.UtcNow.AddMinutes(appointment.Tenant!.CancellationLimitMinutes)) return Results.BadRequest(new { message = "O prazo para remarcar expirou." });
    var appointmentServiceIds = appointment.AppointmentServices.Select(x => x.ServiceId).ToArray();
    if (appointmentServiceIds.Length == 0) appointmentServiceIds = [appointment.ServiceId];
    var result = await booking.Create(appointment.TenantId, user.UserId(), input.BarberId, appointmentServiceIds, input.StartAt, appointment.Notes);
    if (!result.Ok) return result.Conflict ? Results.Conflict(new { message = result.Error }) : Results.BadRequest(new { message = result.Error });
    appointment.Status = AppointmentStatus.Cancelled;
    db.OutboxMessages.Add(new OutboxMessage { TenantId = appointment.TenantId, Type = OutboxMessageType.AppointmentCancelled, Payload = System.Text.Json.JsonSerializer.Serialize(new { appointment.Id, appointment.CustomerId, appointment.BarberId, appointment.StartAtUtc }) });
    await db.SaveChangesAsync();
    metrics.AppointmentCancelled();
    await schedulingCache.InvalidateTenant(appointment.TenantId);
    var rescheduled = result.Appointment!;
    return Results.Ok(new
    {
        rescheduled.Id,
        rescheduled.BarberId,
        rescheduled.ServiceId,
        serviceIds = rescheduled.AppointmentServices.Select(item => item.ServiceId),
        rescheduled.StartAtUtc,
        rescheduled.EndAtUtc,
        status = rescheduled.Status.ToString(),
        rescheduled.Notes
    });
});

app.Run();

public static class ClaimsExtensions
{
    public static Guid UserId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")!);
    public static Guid TenantId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("tenant_id")!);
}

public static class EmailValidator
{
    private static readonly System.Text.RegularExpressions.Regex EmailPattern = new(
        "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public static bool IsValid(string? email) => !string.IsNullOrWhiteSpace(email) && EmailPattern.IsMatch(email);
}

public static class IdempotencyHash
{
    public static string For(CreateAppointmentRequest input, IEnumerable<Guid> serviceIds)
    {
        var value = string.Join('|', input.BarberId, input.StartAt.ToString("O", CultureInfo.InvariantCulture), input.Notes?.Trim(), string.Join(',', serviceIds.OrderBy(x => x)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
