using BarberBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarberBooking.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718120000_HardenSchedulingRules")]
public partial class HardenSchedulingRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SlotIntervalMinutes",
            table: "Tenants",
            type: "integer",
            nullable: false,
            defaultValue: 30);

        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        migrationBuilder.Sql("""
            ALTER TABLE "Appointments"
            ADD CONSTRAINT "CK_Appointments_EndAfterStart"
            CHECK ("EndAtUtc" > "StartAtUtc");
            """);
        migrationBuilder.Sql("""
            ALTER TABLE "Appointments"
            ADD CONSTRAINT "EX_Appointments_NoOverlappingActiveBarberAppointments"
            EXCLUDE USING gist (
                "BarberId" WITH =,
                tstzrange("StartAtUtc", "EndAtUtc", '[)') WITH &&
            ) WHERE ("Status" <> 1);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Appointments\" DROP CONSTRAINT \"EX_Appointments_NoOverlappingActiveBarberAppointments\";");
        migrationBuilder.Sql("ALTER TABLE \"Appointments\" DROP CONSTRAINT \"CK_Appointments_EndAfterStart\";");

        migrationBuilder.DropColumn(
            name: "SlotIntervalMinutes",
            table: "Tenants");
    }
}
