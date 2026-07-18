using BarberBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberBooking.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718123000_AddAppointmentServices")]
public partial class AddAppointmentServices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppointmentServices",
            columns: table => new
            {
                AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                ServiceId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppointmentServices", x => new { x.AppointmentId, x.ServiceId });
                table.ForeignKey(
                    name: "FK_AppointmentServices_Appointments_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Appointments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AppointmentServices_Services_ServiceId",
                    column: x => x.ServiceId,
                    principalTable: "Services",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AppointmentServices_ServiceId",
            table: "AppointmentServices",
            column: "ServiceId");

        migrationBuilder.Sql("""
            INSERT INTO "AppointmentServices" ("AppointmentId", "ServiceId")
            SELECT "Id", "ServiceId"
            FROM "Appointments";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppointmentServices");
    }
}
