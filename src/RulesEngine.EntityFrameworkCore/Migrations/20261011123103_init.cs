using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RulesEngine.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Lookups");

            migrationBuilder.EnsureSchema(
                name: "RuleBuilders");

            migrationBuilder.CreateTable(
                name: "Activity_types",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Create_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Update_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityValidDates",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityValidDates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Diagnoses",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IqviaCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediumDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IqviaType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Create_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Update_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleFields",
                schema: "RuleBuilders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleFormConfigs",
                schema: "RuleBuilders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AllowNestedGroups = table.Column<bool>(type: "bit", nullable: false),
                    StrategyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleFormConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialities",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activity_sub_types",
                schema: "Lookups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IqviaCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediumDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Activity_id = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IqviaType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Update_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Create_timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Valid_DateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity_sub_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activity_sub_types_ActivityValidDates_Valid_DateId",
                        column: x => x.Valid_DateId,
                        principalSchema: "Lookups",
                        principalTable: "ActivityValidDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activity_sub_types_Activity_types_Activity_id",
                        column: x => x.Activity_id,
                        principalSchema: "Lookups",
                        principalTable: "Activity_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleFormConfigFields",
                schema: "RuleBuilders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleformConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RulefieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    AllowedOperatorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleFormConfigFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleFormConfigFields_RuleFields_RulefieldId",
                        column: x => x.RulefieldId,
                        principalSchema: "RuleBuilders",
                        principalTable: "RuleFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RuleFormConfigFields_RuleFormConfigs_RuleformConfigId",
                        column: x => x.RuleformConfigId,
                        principalSchema: "RuleBuilders",
                        principalTable: "RuleFormConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RejectionLevel = table.Column<int>(type: "int", nullable: false),
                    RuleformconfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflows_RuleFormConfigs_RuleformconfigId",
                        column: x => x.RuleformconfigId,
                        principalSchema: "RuleBuilders",
                        principalTable: "RuleFormConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rules_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleActivitySubTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivitySubTypeId = table.Column<long>(type: "bigint", nullable: false),
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
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiagnosisId = table.Column<long>(type: "bigint", nullable: false),
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
                name: "RuleNodes",
                schema: "RuleBuilders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NodeType = table.Column<int>(type: "int", nullable: false),
                    LogicalOperator = table.Column<int>(type: "int", nullable: true),
                    FieldKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Operator = table.Column<int>(type: "int", nullable: true),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleNodes_RuleNodes_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "RuleBuilders",
                        principalTable: "RuleNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RuleNodes_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleSpecialities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                name: "IX_Activity_sub_types_Activity_id",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "Activity_id");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_sub_types_Valid_DateId",
                schema: "Lookups",
                table: "Activity_sub_types",
                column: "Valid_DateId");

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
                name: "IX_RuleFields_Key",
                schema: "RuleBuilders",
                table: "RuleFields",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormConfigFields_RulefieldId",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                column: "RulefieldId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormConfigFields_RuleformConfigId_RulefieldId",
                schema: "RuleBuilders",
                table: "RuleFormConfigFields",
                columns: new[] { "RuleformConfigId", "RulefieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleFormConfigs_Name",
                schema: "RuleBuilders",
                table: "RuleFormConfigs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleNodes_ParentId",
                schema: "RuleBuilders",
                table: "RuleNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleNodes_RuleId",
                schema: "RuleBuilders",
                table: "RuleNodes",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_WorkflowId",
                table: "Rules",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSpecialities_RuleId",
                table: "RuleSpecialities",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSpecialities_SpecialitiesId",
                table: "RuleSpecialities",
                column: "SpecialitiesId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_Name",
                table: "Workflows",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_RuleformconfigId",
                table: "Workflows",
                column: "RuleformconfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuleActivitySubTypes");

            migrationBuilder.DropTable(
                name: "RuleDiagnosis");

            migrationBuilder.DropTable(
                name: "RuleFormConfigFields",
                schema: "RuleBuilders");

            migrationBuilder.DropTable(
                name: "RuleNodes",
                schema: "RuleBuilders");

            migrationBuilder.DropTable(
                name: "RuleSpecialities");

            migrationBuilder.DropTable(
                name: "Activity_sub_types",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "Diagnoses",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "RuleFields",
                schema: "RuleBuilders");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "Specialities",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "ActivityValidDates",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "Activity_types",
                schema: "Lookups");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropTable(
                name: "RuleFormConfigs",
                schema: "RuleBuilders");
        }
    }
}
