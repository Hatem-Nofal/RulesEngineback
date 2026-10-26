using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class rmoveProcedureGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_sub_types_ProcedureGroup_ProcedureGroupId",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.DropTable(
                name: "ProcedureGroup",
                schema: "Lookups");

            migrationBuilder.DropIndex(
                name: "IX_Activity_sub_types_ProcedureGroupId",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.DropColumn(
                name: "ProcedureGroupId",
                schema: "Lookups",
                table: "Activity_sub_types");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcedureGroupId",
                schema: "Lookups",
                table: "Activity_sub_types",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcedureGroup",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ProcedureGroupName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcedureGroupSystemCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedureGroup", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_sub_types_ProcedureGroupId",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "ProcedureGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_sub_types_ProcedureGroup_ProcedureGroupId",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "ProcedureGroupId",
                principalSchema: "Lookups",
                principalTable: "ProcedureGroup",
                principalColumn: "Id");
        }
    }
}
