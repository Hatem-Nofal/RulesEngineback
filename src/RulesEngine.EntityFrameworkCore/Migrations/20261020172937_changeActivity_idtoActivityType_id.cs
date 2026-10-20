using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class changeActivity_idtoActivityType_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_sub_types_Activity_types_Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.RenameColumn(
                name: "Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "ActivityType_id");

            migrationBuilder.RenameIndex(
                name: "IX_Activity_sub_types_Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "IX_Activity_sub_types_ActivityType_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_sub_types_Activity_types_ActivityType_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "ActivityType_id",
                principalSchema: "Lookups",
                principalTable: "Activity_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_sub_types_Activity_types_ActivityType_id",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.RenameColumn(
                name: "ActivityType_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "Activity_id");

            migrationBuilder.RenameIndex(
                name: "IX_Activity_sub_types_ActivityType_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "IX_Activity_sub_types_Activity_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_sub_types_Activity_types_Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "Activity_id",
                principalSchema: "Lookups",
                principalTable: "Activity_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
