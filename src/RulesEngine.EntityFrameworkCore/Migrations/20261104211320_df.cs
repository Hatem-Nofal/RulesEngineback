using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class df : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuleActivitySubTypes");

            migrationBuilder.DropTable(
                name: "RuleDiagnosis");

            migrationBuilder.DropTable(
                name: "RuleSpecialities");

            migrationBuilder.DropColumn(
                name: "ValueJson",
                schema: "RuleBuilders",
                table: "RuleNodes");

            migrationBuilder.AddColumn<long>(
                name: "RuleId",
                schema: "Logers",
                table: "RulesEngineExecutionLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WorkFlowId",
                schema: "Logers",
                table: "RulesEngineExecutionLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RuleId",
                schema: "Logers",
                table: "RulesEngineExecutionLogs");

            migrationBuilder.DropColumn(
                name: "WorkFlowId",
                schema: "Logers",
                table: "RulesEngineExecutionLogs");

            migrationBuilder.AddColumn<string>(
                name: "ValueJson",
                schema: "RuleBuilders",
                table: "RuleNodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RuleActivitySubTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivitySubTypeId = table.Column<long>(type: "bigint", nullable: false),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    IsSecondary = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleActivitySubTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleActivitySubTypes_Activity_sub_types_ActivitySubTypeId",
                        column: x => x.ActivitySubTypeId,
                        principalSchema: "Lookups",
                        principalTable: "Activity_sub_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleActivitySubTypes_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleDiagnosis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiagnosisId = table.Column<long>(type: "bigint", nullable: false),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    IsSecondary = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleDiagnosis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleDiagnosis_Diagnoses_DiagnosisId",
                        column: x => x.DiagnosisId,
                        principalSchema: "Lookups",
                        principalTable: "Diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleDiagnosis_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleSpecialities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    SpecialitiesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSpecialities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleSpecialities_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleSpecialities_Specialities_SpecialitiesId",
                        column: x => x.SpecialitiesId,
                        principalSchema: "Lookups",
                        principalTable: "Specialities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleActivitySubTypes_ActivitySubTypeId",
                table: "RuleActivitySubTypes",
                column: "ActivitySubTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleActivitySubTypes_RuleId",
                table: "RuleActivitySubTypes",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleDiagnosis_DiagnosisId",
                table: "RuleDiagnosis",
                column: "DiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleDiagnosis_RuleId",
                table: "RuleDiagnosis",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSpecialities_RuleId",
                table: "RuleSpecialities",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSpecialities_SpecialitiesId",
                table: "RuleSpecialities",
                column: "SpecialitiesId");
        }
    }
}
