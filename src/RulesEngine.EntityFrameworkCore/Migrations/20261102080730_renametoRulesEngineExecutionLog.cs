using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class renametoRulesEngineExecutionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RulesEngineViolationLog",
                schema: "Logers");

            migrationBuilder.CreateTable(
                name: "RulesEngineExecutionLog",
                schema: "Logers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkFlowName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsViolated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulesEngineExecutionLog", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RulesEngineExecutionLog",
                schema: "Logers");

            migrationBuilder.CreateTable(
                name: "RulesEngineViolationLog",
                schema: "Logers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: true),
                    ClaimNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkFlowName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulesEngineViolationLog", x => x.Id);
                });
        }
    }
}
