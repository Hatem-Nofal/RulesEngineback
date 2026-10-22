using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRuleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Rules");

            migrationBuilder.RenameColumn(
                name: "SuccessEvent",
                table: "Rules",
                newName: "ViolationMessage");

            migrationBuilder.AddColumn<int>(
                name: "ViolationType",
                table: "Rules",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViolationType",
                table: "Rules");

            migrationBuilder.RenameColumn(
                name: "ViolationMessage",
                table: "Rules",
                newName: "SuccessEvent");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Rules",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
