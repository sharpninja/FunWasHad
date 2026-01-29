using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FWH.Mobile.Data.Migrations;

/// <summary>
/// Adds RowVersion column for optimistic concurrency control
/// </summary>
public partial class AddWorkflowRowVersion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "WorkflowDefinitions",
            type: "BLOB",
            rowVersion: true,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "WorkflowDefinitions");
    }
}
