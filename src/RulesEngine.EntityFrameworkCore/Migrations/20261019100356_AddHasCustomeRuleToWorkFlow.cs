using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddHasCustomeRuleToWorkFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RuleformconfigId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<bool>(
                name: "HasCustomeRule",
                table: "Workflows",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCustomeRule",
                table: "Workflows");

            migrationBuilder.AlterColumn<Guid>(
                name: "RuleformconfigId",
                table: "Workflows",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
