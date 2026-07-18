using BarberBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberBooking.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718130000_AddScalabilityInfrastructure")]
public partial class AddScalabilityInfrastructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdempotencyRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "text", nullable: false),
                RequestHash = table.Column<string>(type: "text", nullable: false),
                AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_IdempotencyRequests", x => x.Id));

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Payload = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

        migrationBuilder.DropIndex("IX_Appointments_TenantId", "Appointments");
        migrationBuilder.DropIndex("IX_WorkingHours_BarberId", "WorkingHours");
        migrationBuilder.DropIndex("IX_Services_TenantId", "Services");
        migrationBuilder.CreateIndex("IX_Appointments_BarberId_Status_StartAtUtc", "Appointments", new[] { "BarberId", "Status", "StartAtUtc" });
        migrationBuilder.CreateIndex("IX_Appointments_TenantId_StartAtUtc", "Appointments", new[] { "TenantId", "StartAtUtc" });
        migrationBuilder.CreateIndex("IX_WorkingHours_BarberId_DayOfWeek", "WorkingHours", new[] { "BarberId", "DayOfWeek" });
        migrationBuilder.CreateIndex("IX_Services_TenantId_IsActive", "Services", new[] { "TenantId", "IsActive" });
        migrationBuilder.CreateIndex("IX_IdempotencyRequests_TenantId_CustomerId_Key", "IdempotencyRequests", new[] { "TenantId", "CustomerId", "Key" }, unique: true);
        migrationBuilder.CreateIndex("IX_OutboxMessages_ProcessedAtUtc_CreatedAtUtc", "OutboxMessages", new[] { "ProcessedAtUtc", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "IdempotencyRequests");
        migrationBuilder.DropTable(name: "OutboxMessages");
        migrationBuilder.DropIndex("IX_Appointments_BarberId_Status_StartAtUtc", "Appointments");
        migrationBuilder.DropIndex("IX_Appointments_TenantId_StartAtUtc", "Appointments");
        migrationBuilder.DropIndex("IX_WorkingHours_BarberId_DayOfWeek", "WorkingHours");
        migrationBuilder.DropIndex("IX_Services_TenantId_IsActive", "Services");
        migrationBuilder.CreateIndex("IX_Appointments_TenantId", "Appointments", "TenantId");
        migrationBuilder.CreateIndex("IX_WorkingHours_BarberId", "WorkingHours", "BarberId");
        migrationBuilder.CreateIndex("IX_Services_TenantId", "Services", "TenantId");
    }
}
