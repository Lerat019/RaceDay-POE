using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDayApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Event",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Run");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Event_EventType",
                table: "Event",
                sql: "EventType IN ('Run', 'Walk', 'Cycle')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Event_EventType",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Event");
        }
    }
}
