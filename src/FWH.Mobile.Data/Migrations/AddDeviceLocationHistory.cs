using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FWH.Mobile.Data.Migrations;

/// <summary>
/// Adds DeviceLocationHistory table for local device location tracking.
/// TR-MOBILE-001: Device location tracked locally, never sent to API.
/// </summary>
public partial class AddDeviceLocationHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeviceLocationHistory",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Latitude = table.Column<double>(type: "REAL", nullable: false),
                Longitude = table.Column<double>(type: "REAL", nullable: false),
                AccuracyMeters = table.Column<double>(type: "REAL", nullable: true),
                AltitudeMeters = table.Column<double>(type: "REAL", nullable: true),
                SpeedMetersPerSecond = table.Column<double>(type: "REAL", nullable: true),
                HeadingDegrees = table.Column<double>(type: "REAL", nullable: true),
                MovementState = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeviceLocationHistory", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DeviceLocationHistory_DeviceId",
            table: "DeviceLocationHistory",
            column: "DeviceId");

        migrationBuilder.CreateIndex(
            name: "IX_DeviceLocationHistory_Timestamp",
            table: "DeviceLocationHistory",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_DeviceLocationHistory_DeviceId_Timestamp",
            table: "DeviceLocationHistory",
            columns: new[] { "DeviceId", "Timestamp" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DeviceLocationHistory");
    }
}
