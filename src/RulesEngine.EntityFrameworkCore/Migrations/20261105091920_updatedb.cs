using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class updatedb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields");

            migrationBuilder.DropColumn(
                name: "Order",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
