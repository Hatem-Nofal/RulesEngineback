using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class removeUniqinRuleFormConfigField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuleFormConfigFields_RuleformConfigId_RulefieldId",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields");

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormConfigFields_RuleformConfigId_RulefieldId",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                columns: new[] { "RuleformConfigId", "RulefieldId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuleFormConfigFields_RuleformConfigId_RulefieldId",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields");

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormConfigFields_RuleformConfigId_RulefieldId",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                columns: new[] { "RuleformConfigId", "RulefieldId" },
                unique: true);
        }
    }
}
