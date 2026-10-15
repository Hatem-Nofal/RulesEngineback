using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class addisMultipletoRuleFormConfigField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_sub_types_Activity_types_Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.RenameColumn(
                name: "Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "ActivityTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Activity_sub_types_Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "IX_Activity_sub_types_ActivityTypeId");

            migrationBuilder.AddColumn<bool>(
                name: "IsMultiple",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_sub_types_Activity_types_ActivityTypeId",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "ActivityTypeId",
                principalSchema: "Lookups",
                principalTable: "Activity_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activity_sub_types_Activity_types_ActivityTypeId",
                schema: "Lookups",
                table: "Activity_sub_types");

            migrationBuilder.DropColumn(
                name: "IsMultiple",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields");

            migrationBuilder.RenameColumn(
                name: "ActivityTypeId",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "Activity_id");

            migrationBuilder.RenameIndex(
                name: "IX_Activity_sub_types_ActivityTypeId",
                schema: "Lookups",
                table: "Activity_sub_types",
                newName: "IX_Activity_sub_types_Activity_id");

            migrationBuilder.AddColumn<int>(
                name: "ActivityId",
                schema: "Lookups",
                table: "Activity_sub_types",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
