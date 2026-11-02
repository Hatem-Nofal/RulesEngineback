using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class renametoRulesEngineExecutionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RulesEngineExecutionLog",
                schema: "Logers",
                table: "RulesEngineExecutionLog");

            migrationBuilder.RenameTable(
                name: "RulesEngineExecutionLog",
                schema: "Logers",
                newName: "RulesEngineExecutionLogs",
                newSchema: "Logers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RulesEngineExecutionLogs",
                schema: "Logers",
                table: "RulesEngineExecutionLogs",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RulesEngineExecutionLogs",
                schema: "Logers",
                table: "RulesEngineExecutionLogs");

            migrationBuilder.RenameTable(
                name: "RulesEngineExecutionLogs",
                schema: "Logers",
                newName: "RulesEngineExecutionLog",
                newSchema: "Logers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RulesEngineExecutionLog",
                schema: "Logers",
                table: "RulesEngineExecutionLog",
                column: "Id");
        }
    }
}
