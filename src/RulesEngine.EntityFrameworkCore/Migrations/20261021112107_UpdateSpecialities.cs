using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpecialities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "Lookups",
                table: "Specialities",
                newName: "SpecialityCode");

            migrationBuilder.RenameColumn(
                name: "Code",
                schema: "Lookups",
                table: "Specialities",
                newName: "SpecialityDisplay");

            migrationBuilder.AddColumn<string>(
                name: "Group",
                schema: "Lookups",
                table: "Specialities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialityDescription",
                schema: "Lookups",
                table: "Specialities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                schema: "Lookups",
                table: "Specialities");

            migrationBuilder.DropColumn(
                name: "SpecialityDescription",
                schema: "Lookups",
                table: "Specialities");

            migrationBuilder.RenameColumn(
                name: "SpecialityDisplay",
                schema: "Lookups",
                table: "Specialities",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "SpecialityCode",
                schema: "Lookups",
                table: "Specialities",
                newName: "Description");
        }
    }
}
