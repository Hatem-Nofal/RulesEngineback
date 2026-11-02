using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class changeisWorkflowLevelToIsWorkflowLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isWorkflowLevel",
                table: "Workflows",
                newName: "IsWorkflowLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsWorkflowLevel",
                table: "Workflows",
                newName: "isWorkflowLevel");
        }
    }
}
