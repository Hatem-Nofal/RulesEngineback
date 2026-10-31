using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class addActitvtyToRulesEngineViolationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Activity",
                schema: "Logers",
                table: "RulesEngineViolationLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activity",
                schema: "Logers",
                table: "RulesEngineViolationLog");
        }
    }
}
