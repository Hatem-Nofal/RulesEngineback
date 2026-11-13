using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.Lookups;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Integrations.RulesEngineManagers;
using RulesEngine.Permissions;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.Helpers;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
namespace RulesEngine.WorkFlows.Rules
{

    [Authorize(RulesEnginePermissions.Rules)]
    public class RuleAppService : CrudAppService<Rule, RuleDto, RuleListItemDto, long, RulePagedAndSortedResultRequestDto, CreateRuleDto, CreateRuleDto>, IRuleAppService
    {
        private readonly IRepository<RuleNode, Guid> _ruleNodeRepository;
        private readonly IWorkFlowRepository _workflowRepository;
        private readonly IRepository<RuleField, long> _ruleFieldRepository;
        private readonly IRuleExpressionBuilder _expressionsBuilder;
        private readonly IRuleRepository _ruleRepository;
        private readonly IRepository<ActivitySubType> _activitySubTypeRepository;
        private readonly IRepository<Diagnosis> _diagnosisRepository;
        private readonly IRepository<Specialities> _specialitiesRepository;
        private readonly IRepository<GenericDrug> _genericDrugsRepository;
        private readonly IRepository<MorbidityGroup, int> _morbidityGroupRepository;
        private const string ErrorsHeader = "Errors";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private IRulesEngineManager EngineManager => LazyServiceProvider.LazyGetRequiredService<IRulesEngineManager>();

        private static string GetFieldHeaderLabel(string fieldKey, int occurrenceIndex, int totalOccurrences)
        {
            if (totalOccurrences <= 1)
                return fieldKey;

            return $"{fieldKey} ({GetDuplicateName(occurrenceIndex)})";
        }

        private static string GetDuplicateName(int occurrenceIndex)
        {
            return occurrenceIndex switch
            {
                1 => "Primary",
                2 => "Secondary",
                3 => "Tertiary",
                4 => "Fourth",
                5 => "Fifth",
                6 => "Sixth",
                7 => "Seventh",
                8 => "Eighth",
                9 => "Ninth",
                _ => occurrenceIndex.ToString()
            };
        }

        private static string NormalizeFieldKey(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return string.Empty;

            var trimmed = header.Trim();
            if (trimmed.EndsWith("Operators", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(0, trimmed.Length - "Operators".Length).TrimEnd();

            var start = trimmed.LastIndexOf('(');
            if (start < 0)
                return trimmed;

            var end = trimmed.LastIndexOf(')');
            if (end <= start)
                return trimmed;

            var label = trimmed.Substring(start + 1, end - start - 1).Trim();
            if (!IsDuplicateLabel(label))
                return trimmed;

            return trimmed.Substring(0, start).TrimEnd();
        }

        private static bool IsDuplicateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return false;

            return label switch
            {
                "Primary" => true,
                "Secondary" => true,
                "Tertiary" => true,
                "Fourth" => true,
                "Fifth" => true,
                "Sixth" => true,
                "Seventh" => true,
                "Eighth" => true,
                "Ninth" => true,
                _ when int.TryParse(label, out _) => true,
                _ => false
            };
        }

        private static int GetFieldOccurrenceIndex(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return 1;

            var trimmed = header.Trim();
            if (trimmed.EndsWith("Operators", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(0, trimmed.Length - "Operators".Length).TrimEnd();

            var start = trimmed.LastIndexOf('(');
            if (start < 0)
                return 1;

            var end = trimmed.LastIndexOf(')');
            if (end <= start)
                return 1;

            var label = trimmed.Substring(start + 1, end - start - 1).Trim();
            return label switch
            {
                "Primary" => 1,
                "Secondary" => 2,
                "Tertiary" => 3,
                "Fourth" => 4,
                "Fifth" => 5,
                "Sixth" => 6,
                "Seventh" => 7,
                "Eighth" => 8,
                "Ninth" => 9,
                _ when int.TryParse(label, out var numeric) => numeric,
                _ => 1
            };
        }

        private static (RuleFormConfigField Config, RuleField Field)? ResolveAllowedField(
            string fieldKey,
            int occurrenceIndex,
            IReadOnlyList<(RuleFormConfigField Config, RuleField Field)> allowedByKey)
        {
            var matches = allowedByKey
                .Where(x => string.Equals(x.Field.Key, fieldKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
                return null;

            var index = Math.Max(0, Math.Min(occurrenceIndex - 1, matches.Count - 1));
            return matches[index];
        }

        public RuleAppService(
            IRepository<Rule, long> repository,
            IRepository<RuleNode, Guid> ruleNodeRepository,
           IWorkFlowRepository workflowRepository,
             IRuleExpressionBuilder expressionsBuilder,
            IRuleRepository ruleRepository,
            IRepository<RuleField, long> ruleFieldRepository,
            IRepository<ActivitySubType> activitySubTypeRepository,
            IRepository<Diagnosis> diagnosisRepository,
            IRepository<Specialities> specialitiesRepository,
            IRepository<GenericDrug> genericDrugsRepository,
            IRepository<MorbidityGroup, int> morbidityGroupRepository) : base(repository)
        {
            _ruleNodeRepository = ruleNodeRepository;
            _workflowRepository = workflowRepository;
            _expressionsBuilder = expressionsBuilder;
            _ruleRepository = ruleRepository;
            _ruleFieldRepository = ruleFieldRepository;
            _activitySubTypeRepository = activitySubTypeRepository;
            _diagnosisRepository = diagnosisRepository;
            _specialitiesRepository = specialitiesRepository;
            _genericDrugsRepository = genericDrugsRepository;
            _morbidityGroupRepository = morbidityGroupRepository;
        }

        public override async Task<PagedResultDto<RuleListItemDto>> GetListAsync(RulePagedAndSortedResultRequestDto input)
        {
            // Paging happens in SQL and only the four list columns are read. The previous version loaded every rule
            // (including its expression text) into memory just to take one page of it.
            var query = (await Repository.GetQueryableAsync())
                .WhereIf(!string.IsNullOrEmpty(input.RuleName), x => x.Name.Contains(input.RuleName))
                .WhereIf(input.WorkflowId.HasValue, x => x.WorkflowId == input.WorkflowId);   // stable order, so a row can never appear on two pages

            var total = await AsyncExecuter.CountAsync(query);
            query = query.OrderBy(input.Sorting ?? "CreationTime DESC")
                .PageBy(input);



            var workflows = (await _workflowRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

            var items = query.Select(x => new RuleListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                WorkflowId = x.WorkflowId,
                WorkflowName = workflows.GetValueOrDefault(x.WorkflowId),
                IsActive = x.IsActive
            }).ToList();

            return new PagedResultDto<RuleListItemDto>(total, items);
        }

        public override async Task<RuleDto> GetAsync(long id)
        {
            var rule = await _ruleRepository.GetAsync(id)
                       ?? throw new KeyNotFoundException("Rule not found.");
            var formConfig = await _expressionsBuilder.LoadFormConfigAsync(rule.WorkflowId);

            return new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                WorkflowId = rule.WorkflowId,
                IsActive = rule.IsActive,
                Root = !rule.Workflow.IsWorkflowLevel ? await _expressionsBuilder.ReverseBuild(rule.Expression, formConfig.Fields) : new RuleNodeDto(),
                Expression = rule.Expression,
                WorkflowIsWorkflowLevel = rule.Workflow.IsWorkflowLevel,
                ViolationMessage = rule.ViolationMessage ?? rule.Workflow.ViolationMessage,
                ViolationType = rule.ViolationType
            };
        }

        public override async Task<RuleDto> CreateAsync(CreateRuleDto input)
        {
            return await SaveRuleAsync(input, null);
        }

        public override async Task<RuleDto> UpdateAsync(long id, CreateRuleDto input)
        {
            return await UpdateRuleAsync(input, id);
        }

        public override async Task DeleteAsync(long id)
        {
            var rule = await Repository.GetAsync(x => x.Id == id);

            // One set-based DELETE for all nodes instead of loading them and deleting them one by one.
            await _ruleNodeRepository.DeleteDirectAsync(n => n.WorkFlowId == id);

            await Repository.HardDeleteAsync(rule);
            await EngineManager.ReloadAfterCommitAsync();
        }

        /// <summary>Validated rule definition: everything needed to save, computed without any database write.</summary>
        private sealed record PreparedRule(RuleDto Dto, string Expression);

        /// <summary>
        /// Loads the workflow's form config as a DTO: one query for the workflow (with its config and config fields)
        /// and one for the field catalog. Code that handles many rules (bulk import) calls this once and reuses the result.
        /// </summary>


        /// <summary>Pure mapping; every field is found through a dictionary instead of a linear scan per property.</summary>

        /// <summary>
        /// Validates the rule tree against the workflow's form config and builds the expression. Never writes.
        /// Pass <paramref name="formConfig"/> when preparing many rules of the same workflow so it is loaded only once.
        /// </summary>
        private async Task<PreparedRule> PrepareRuleAsync(CreateRuleDto input, RuleFormConfigDto? formConfig = null)
        {


            formConfig ??= await _expressionsBuilder.LoadFormConfigAsync(input.WorkflowId);

            RuleTreeValidator.Validate(input.Root, formConfig);
            var ruleDto = ObjectMapper.Map<CreateRuleDto, RuleDto>(input);
            var expressionResult = await _expressionsBuilder.BuildForRuleAsync(ruleDto, formConfig);

            return new PreparedRule(ruleDto, expressionResult.Expression);
        }
        /// <param name="saveChanges">
        /// true (default): flush right away. Bulk import passes false and flushes once after the last rule.
        /// </param>
        private async Task<RuleDto> SaveRuleAsync(CreateRuleDto input, PreparedRule? prepared = null, bool saveChanges = true)
        {
            prepared ??= await PrepareRuleAsync(input);
            var ruleDto = prepared.Dto;



            var workflow = await _workflowRepository.GetAsync(input.WorkflowId);
            var rule = new Rule(input.WorkflowId, prepared.Expression, input.ViolationMessage ?? workflow.ViolationMessage);

            var count = await _ruleRepository.CountAsync(x => x.WorkflowId == input.WorkflowId);


            if (input.IsActive) rule.Activate(); else rule.Deactivate();


            rule.GenerateUniqueNameAsync(count, workflow.Name);

            rule.AddRuleAuditLog(RuleAuditLog.Create(rule.Id, rule, rule.ViolationType, rule.ViolationMessage, rule.IsActive, prepared.Expression));

            await _ruleRepository.InsertAsync(rule);

            await CurrentUnitOfWork.SaveChangesAsync();   // flush to get the rule ID for the nodes
            await EngineManager.ReloadAfterCommitAsync();   // once per unit of work, even for bulk uploads
            return ruleDto;
        }
        private async Task<RuleDto> UpdateRuleAsync(CreateRuleDto input, long id, PreparedRule? prepared = null, bool saveChanges = true)
        {
            prepared ??= await PrepareRuleAsync(input);
            var ruleDto = prepared.Dto;
            try
            {

                _ruleRepository.DisableTracking();
                var rule = await _ruleRepository.GetForUpdateAsync(id);


                rule.Update(prepared.Expression, input.ViolationMessage, input.ViolationType);

                // Replace the whole tree: one set-based DELETE instead of loading and deleting every node separately.


                if (input.IsActive) rule.Activate(); else rule.Deactivate();

                rule.AddRuleAuditLog(RuleAuditLog.Create(rule.Id, rule, rule.ViolationType, rule.ViolationMessage, rule.IsActive, prepared.Expression));


                await _ruleRepository.UpdateAsync(rule);


                await EngineManager.ReloadAfterCommitAsync();
            }
            catch (Exception ex)
            {

                throw;
            } // once per unit of work, even for bulk uploads
            return ruleDto;
        }

        public async Task<RuleExpressionResultDto> PreviewExpression(RuleDto input)
        {

            var ss = JsonSerializer.Serialize(input);

            return await _expressionsBuilder.BuildForRuleAsync(input);
        }


        public async Task<IRemoteStreamContent> GetRulesSampleAsync(long workflowId)
        {
            var workflow = await _workflowRepository.GetAsync(workflowId)
                ?? throw new KeyNotFoundException("Workflow not found.");

            var config = workflow.RuleformConfig;

            if (config == null)
                throw new RuleValidationException(
                    "This workflow has no form config assigned.");

            var configFields = config.RuleFormConfigFields
                .OrderBy(f => f.CreationTime)
                .ToList();

            var configFieldIds = configFields
                .Select(x => x.RulefieldId)
                .ToList();

            var fields = (await _ruleFieldRepository.GetListAsync(
                x => configFieldIds.Contains(x.Id)))
                .OrderBy(f => configFields.FindIndex(
                    cf => cf.RulefieldId == f.Id))
                .ToList();
            var lookup = fields.ToDictionary(x => x.Id);

            var fieldsList = configFieldIds
                .Where(id => lookup.ContainsKey(id))
                .Select(id => lookup[id])
                .ToList();
            var layout = workflow.IsWorkflowLevel
                ? null
                : RuleLayout.FromNodes(await _ruleNodeRepository.GetListAsync(n => n.WorkFlowId == workflowId));

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("RulesSample");
            var optionsSheet = workbook.Worksheets.Add("Options");

            // Headers
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "IsActive";
            ws.Cell(1, 3).Value = "ID";

            var duplicateCounts = fieldsList
                .GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var keyOccurrence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fieldsList.Count; i++)
            {
                var field = fieldsList[i];
                var occurrenceIndex = 1;

                if (duplicateCounts.TryGetValue(field.Key, out var total) && total > 1)
                {
                    occurrenceIndex = keyOccurrence[field.Key] =
                        keyOccurrence.GetValueOrDefault(field.Key) + 1;
                }
                var totalOccurrences = duplicateCounts.TryGetValue(field.Key, out var count) ? count : 1;
                var fieldHeader = GetFieldHeaderLabel(field.Key, occurrenceIndex, totalOccurrences);

                ws.Cell(1, 4 + i * 2).Value = fieldHeader;
                ws.Cell(1, 5 + i * 2).Value = fieldHeader + "Operators";
            }

            ws.Row(1).Style.Font.SetBold();

            // Add dropdowns for fields with OptionsJson.
            // Each field gets its own options column and named range.
            const int lastRowValidation = 1000;

            for (int i = 0; i < fieldsList.Count; i++)
            {
                var f = fieldsList[i];

                if (string.IsNullOrWhiteSpace(f.OptionsJson))
                    continue;

                List<string> options;

                try
                {
                    options = JsonSerializer.Deserialize<List<string>>(
                        f.OptionsJson) ?? new List<string>();
                }
                catch (JsonException)
                {
                    continue;
                }

                if (options.Count == 0)
                    continue;

                // Store this field's options in its own column on the hidden sheet.
                int optionsCol = i + 1;

                for (int j = 0; j < options.Count; j++)
                {
                    optionsSheet.Cell(j + 1, optionsCol).Value = options[j];
                }

                string optionsColumnLetter =
                    XLHelper.GetColumnLetterFromNumber(optionsCol);

                string rangeName = $"FieldOptions{i + 1}";

                workbook.DefinedNames.Add(
                    rangeName,
                    $"'Options'!${optionsColumnLetter}$1:" +
                    $"${optionsColumnLetter}${options.Count}"
                );

                // A=Name, B=IsActive, C=ID, D onwards= field and operator pairs.
                int excelCol = 4 + i * 2; // field value column

                var validation = ws.Range(
                    2, excelCol,
                    lastRowValidation, excelCol)
                    .CreateDataValidation();

                validation.List(rangeName);
                validation.InCellDropdown = true;
                validation.IgnoreBlanks = true;
                validation.ShowErrorMessage = true;

                // Add validation for the operator column (adjacent column)
                int operatorCol = excelCol + 1;
                var opValidation = ws.Range(2, operatorCol, lastRowValidation, operatorCol).CreateDataValidation();
                opValidation.InCellDropdown = true;
                opValidation.IgnoreBlanks = true;
                opValidation.ShowErrorMessage = true;
            }

            // Add operator options in the Options sheet and a named range.
            var operatorNames = Enum.GetNames(typeof(RuleOperator)).ToList();
            int operatorOptionsCol = fieldsList.Count + 1; // after per-field options columns
            for (int j = 0; j < operatorNames.Count; j++)
            {
                optionsSheet.Cell(j + 1, operatorOptionsCol).Value = operatorNames[j];
            }

            string operatorOptionsColumnLetter = XLHelper.GetColumnLetterFromNumber(operatorOptionsCol);
            workbook.DefinedNames.Add(
                "OperatorOptions",
                $"'Options'!${operatorOptionsColumnLetter}$1:${operatorOptionsColumnLetter}${operatorNames.Count}"
            );

            // Assign the operator named range to each operator column we created earlier.
            for (int i = 0; i < fieldsList.Count; i++)
            {
                int operatorCol = 5 + i * 2; // matching header placement
                var opValidation = ws.Range(2, operatorCol, lastRowValidation, operatorCol).CreateDataValidation();
                opValidation.List("OperatorOptions");
                opValidation.InCellDropdown = true;
                opValidation.IgnoreBlanks = true;
                opValidation.ShowErrorMessage = true;
            }

            // Hide the sheet containing dropdown options.
            optionsSheet.Hide();

            // Show how the columns are grouped; bulk upload rebuilds every rule with exactly this shape.
            if (layout != null)
            {
                var layoutSheet = workbook.Worksheets.Add("Layout");
                layoutSheet.Cell(1, 1).Value = "How the columns are grouped in each generated rule";
                layoutSheet.Cell(1, 1).Style.Font.SetBold();
                layoutSheet.Cell(2, 1).Value = layout.Describe(
                    (key, occurrence) => GetFieldHeaderLabel(key, occurrence, duplicateCounts.GetValueOrDefault(key, 1)));
                layoutSheet.Cell(2, 1).Style.Alignment.WrapText = true;
                layoutSheet.Column(1).Width = 120;
            }

            // Populate sample row or existing rules.
            var rules = (await Repository.GetListAsync(
                r => r.WorkflowId == workflowId))
                .ToList();

            int row = 2;

            if (!rules.Any())
            {
                ws.Cell(row, 1).Value = "Sample rule";
                ws.Cell(row, 2).Value = true;

                for (int i = 0; i < fieldsList.Count; i++)
                {
                    var f = fieldsList[i];
                    int fieldCol = 4 + i * 2;
                    int operatorCol = fieldCol + 1;

                    // default operator = Equal, default value = first option (if available)
                    var occurrence = fieldsList.Take(i + 1).Count(x => string.Equals(x.Key, f.Key, StringComparison.OrdinalIgnoreCase));
                    ws.Cell(row, operatorCol).Value = (layout?.DefaultOperatorFor(f.Key, occurrence) ?? RuleOperator.Equal).ToString();

                    string value = string.Empty;

                    if (!string.IsNullOrWhiteSpace(f.OptionsJson))
                    {
                        try
                        {
                            var opts = JsonSerializer.Deserialize<List<string>>(
                                f.OptionsJson);

                            if (opts?.Count > 0)
                                value = opts[0];
                        }
                        catch (JsonException)
                        {
                            // Leave the sample value empty if JSON is invalid.
                        }
                    }

                    ws.Cell(row, fieldCol).Value = value;
                }

            }
            else
            {
                var formConfig = await _expressionsBuilder.LoadFormConfigAsync(workflowId);

                foreach (var rule in rules)
                {
                    var rootNode = await _expressionsBuilder.ReverseBuild(rule.Expression, formConfig.Fields);

                    var nodeByFieldKey = new Dictionary<string, Queue<RuleNodeDto>>(StringComparer.OrdinalIgnoreCase);
                    FlattenRuleTree(rootNode, nodeByFieldKey);

                    ws.Cell(row, 1).Value = rule.Name;
                    ws.Cell(row, 2).Value = rule.IsActive;
                    ws.Cell(row, 3).Value = rule.Id.ToString();

                    for (int i = 0; i < fieldsList.Count; i++)
                    {
                        var f = fieldsList[i];
                        int fieldCol = 4 + i * 2;
                        int operatorCol = fieldCol + 1;

                        var node = nodeByFieldKey.TryGetValue(f.Key, out var queue) && queue.Count > 0
                            ? queue.Dequeue()
                            : null;

                        if (node != null)
                        {
                            if (node.Operator != null)
                                ws.Cell(row, operatorCol).Value = node.Operator.Value.ToString();

                            string cellValue = string.Empty;

                            if (node.Value.HasValue)
                            {
                                var element = node.Value.Value;
                                if (element.ValueKind == JsonValueKind.Array)
                                {
                                    cellValue = string.Join(",", element.EnumerateArray().Select(x => x.ToString()));
                                }
                                else if (element.ValueKind == JsonValueKind.String)
                                {
                                    cellValue = element.GetString() ?? string.Empty;
                                }
                                else
                                {
                                    cellValue = element.ToString();
                                }
                            }

                            ws.Cell(row, fieldCol).Value = cellValue;
                        }
                        else
                        {
                            ws.Cell(row, fieldCol).Value = string.Empty;
                            ws.Cell(row, operatorCol).Value = string.Empty;
                        }
                    }

                    static void FlattenRuleTree(RuleNodeDto node, IDictionary<string, Queue<RuleNodeDto>> nodeByFieldKey)
                    {
                        if (node == null)
                            return;

                        if (node.NodeType == RuleNodeType.Condition && !string.IsNullOrWhiteSpace(node.FieldKey))
                        {
                            if (!nodeByFieldKey.TryGetValue(node.FieldKey, out var queue))
                                nodeByFieldKey[node.FieldKey] = queue = new Queue<RuleNodeDto>();

                            queue.Enqueue(node);
                        }

                        foreach (var child in node.Children)
                            FlattenRuleTree(child, nodeByFieldKey);
                    }

                    row++;
                }

                // Basic formatting
                ws.Columns().AdjustToContents();

                // Preserve the dropdown columns' width within reasonable limits.
                for (int col = 1; col <= 3 + fieldsList.Count * 2; col++)
                {
                    if (ws.Column(col).Width > 40)
                        ws.Column(col).Width = 40;
                }
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return new RemoteStreamContent(
                stream,
                "Rules_Import_Template.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            );
        }

        /// <summary>
        /// Imports rules from an Excel sheet (see <see cref="GetRulesSampleAsync"/>).
        /// Every row is validated first and nothing is saved unless ALL rows are valid.
        /// Returns null on success; when any row has a problem, returns the same workbook with the problems
        /// written to an "Errors" column at the end of each affected row (several problems: comma separated).
        /// </summary>
        /// <summary>
        /// Imports rules from a sheet: all-or-nothing. If any row has a problem nothing is saved and the user gets their
        /// own sheet back with only the bad rows and an "Errors" column.
        /// Database round trips are constant, not per row/cell: one query each for the workflow, its layout, the field
        /// catalog, the existing rule names, the rules being updated, and one per kind of code (Activity, Diagnoses, ...).
        /// </summary>
        public async Task<IRemoteStreamContent?> BulkUploadAsync(long workflowId, IRemoteStreamContent file)
        {
            if (file == null || file.ContentLength == 0) return null;

            var ruleFields = await _ruleFieldRepository.GetListAsync();
            var fieldsById = ruleFields.ToDictionary(x => x.Id);

            using var buffer = new MemoryStream();
            await using (var upload = file.GetStream())
            {
                await upload.CopyToAsync(buffer);
            }
            buffer.Position = 0;

            using var wb = new XLWorkbook(buffer);
            var ws = wb.Worksheets.First();

            if (ws.FirstRowUsed() == null) return null;

            // Real column numbers (a blank header cell in the middle must not shift the columns after it).
            var headers = ws.Row(1).CellsUsed()
                .Select(c => (Column: c.Address.ColumnNumber, Raw: c.GetString()?.Trim() ?? string.Empty))
                .ToList();
            if (headers.Count == 0) return null;

            // Operator columns are named '<FieldKey>Operators' (e.g. 'AgeOperators').
            const string operatorSuffix = "Operators";
            var operatorColumnByField = new Dictionary<(string FieldKey, int Occurrence), int>();
            foreach (var (column, raw) in headers)
            {
                if (!raw.EndsWith(operatorSuffix, StringComparison.OrdinalIgnoreCase)) continue;
                var fieldKey = raw.Substring(0, raw.Length - operatorSuffix.Length).TrimEnd();
                operatorColumnByField[(NormalizeFieldKey(fieldKey), GetFieldOccurrenceIndex(fieldKey))] = column;
            }

            // Workflow + form config + config fields only. (GetDetailsAsync also joins every existing rule and node:
            // for a workflow with thousands of rules that is a huge, useless result set.)
            var workflow = await _workflowRepository.GetAsync(workflowId);
            var config = workflow.RuleformConfig;
            if (config == null) throw new RuleValidationException("This workflow has no form config assigned.");

            // Built once for the whole sheet: used to validate every row and to build every expression.
            var formConfigDto = RuleExpressionBuilder.BuildFormConfigDto(config, fieldsById);
            var fieldMap = formConfigDto.Fields
                .Select(f => new FieldMapDto(
                    f.Key ?? throw new RuleValidationException(
                        $"Field '{f.Label}' has no context path in form config '{formConfigDto.Name}'."),
                    FieldMapDto.KindOf(f.DataType)))
                .ToList();

            // The workflow's saved rule layout (groups + AND/OR joins): each row is poured into this shape so the
            // generated expression keeps its grouping instead of becoming one flat AND list.
            var layout = workflow.IsWorkflowLevel
                ? null
                : RuleLayout.FromNodes(await _ruleNodeRepository.GetListAsync(n => n.WorkFlowId == workflowId));

            var allowedByKey = config.RuleFormConfigFields
                .Select(configField => (Config: configField, Field: fieldsById[configField.RulefieldId]))
                .ToList();

            if (allowedByKey.Count == 0)
                throw new RuleValidationException("This workflow has no valid form fields assigned.");

            int FindColumn(string name) => headers
                .FirstOrDefault(h => string.Equals(NormalizeFieldKey(h.Raw), name, StringComparison.OrdinalIgnoreCase)).Column;

            var nameColumn = FindColumn("Name");
            var isActiveColumn = FindColumn("IsActive");
            var idColumn = FindColumn("ID");

            // Without a Name column no row can ever be imported; say so instead of silently doing nothing.
            if (nameColumn == 0)
                throw new RuleValidationException("The sheet must contain a 'Name' column.");

            // Resolve every header ONCE (field, occurrence, operator column, code kind) instead of once per cell.
            var fieldColumns = new List<SheetColumn>();
            foreach (var (column, raw) in headers)
            {
                var key = NormalizeFieldKey(raw);
                if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "IsActive", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "ID", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, ErrorsHeader, StringComparison.OrdinalIgnoreCase)) continue;

                var occurrence = GetFieldOccurrenceIndex(raw);
                var allowed = ResolveAllowedField(key, occurrence, allowedByKey);
                if (allowed == null) continue;

                fieldColumns.Add(new SheetColumn(
                    column, key, occurrence, allowed.Value.Config, allowed.Value.Field,
                    operatorColumnByField.TryGetValue((key, occurrence), out var opColumn) ? opColumn : 0,
                    layout?.DefaultOperatorFor(key, occurrence),
                    GetCodeKind(key)));
            }

            var rows = new List<UploadRow>();
            var pendingCodes = new PendingCodeChecks<UploadRow>();

            // ---- Phase 1a: read every row. Nothing is written to the database. ----
            foreach (var xlRow in ws.RowsUsed().Where(x => x.RowNumber() > 1))
            {
                var row = new UploadRow(xlRow.RowNumber());
                rows.Add(row);

                if (idColumn > 0)
                {
                    var idText = xlRow.Cell(idColumn).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(idText))
                    {
                        if (long.TryParse(idText, out var parsedId)) row.ExistingId = parsedId;
                        else row.Errors.Add($"Invalid ID '{idText}' (expected a long).");
                    }
                }

                if (isActiveColumn > 0)
                {
                    var text = xlRow.Cell(isActiveColumn).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (bool.TryParse(text, out var parsedActive)) row.IsActive = parsedActive;
                        else row.Errors.Add($"Invalid IsActive '{text}' (expected True or False).");
                    }
                }

                foreach (var col in fieldColumns)
                {
                    var val = xlRow.Cell(col.Column).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    // Per-row operator column ('<FieldKey>Operators'); blank = the layout's operator, else Equal.
                    var operatorToUse = col.DefaultOperator ?? RuleOperator.Equal;
                    if (col.OperatorColumn > 0)
                    {
                        var opCellStr = xlRow.Cell(col.OperatorColumn).GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(opCellStr))
                        {
                            if (Enum.TryParse<RuleOperator>(opCellStr, true, out var parsedOp)) operatorToUse = parsedOp;
                            else row.Errors.Add($"Invalid operator '{opCellStr}' for '{col.Key}'.");
                        }
                    }

                    if (val.Equals(operatorToUse.ToString())) continue;

                    var node = new RuleNodeDto
                    {
                        NodeType = RuleNodeType.Condition,
                        FieldKey = col.Key,
                        Operator = operatorToUse,
                        IsMultiple = col.Config.IsMultiple
                    };

                    // SerializeToElement: no JsonDocument to dispose (Parse(...).RootElement leaks a pooled buffer per cell).
                    if (col.Config.IsMultiple || val.Contains('|') || (col.Field.DataType == "List" && val.Contains(',')))
                    {
                        var parts = val.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).ToArray();
                        node.Value = JsonSerializer.SerializeToElement(parts);
                    }
                    else
                    {
                        node.Value = JsonSerializer.SerializeToElement(val);
                    }

                    row.Conditions.Add(new LayoutCondition(col.Key, col.Occurrence, node));

                    if (col.CodeKind is { } kind)
                        pendingCodes.Add(kind, val, row);
                }
            }

            // ---- Phase 1b: verify every code of the sheet with a few set-based queries (not one per cell). ----
            foreach (var kind in pendingCodes.Kinds)
            {
                var existing = await LoadExistingCodesAsync(kind, pendingCodes.CodesOf(kind));
                pendingCodes.ReportMissing(kind, existing, (row, code) => row.Errors.Add(MissingCodeMessage(kind, code)));
            }

            // ---- Phase 1c: build + validate each clean row's tree and expression (pure CPU, no database). ----
            foreach (var row in rows.Where(x => x.Errors.Count == 0))
            {
                var root = layout != null ? layout.Bind(row.Conditions) : RuleLayout.BuildFlat(row.Conditions);

                // Report every problem of the row at once, so the user can fix the whole row in a single pass.
                var treeErrors = RuleTreeValidator.ValidateWitErrors(root, formConfigDto);
                if (treeErrors.Count > 0)
                {
                    row.Errors.AddRange(treeErrors.Distinct());
                    continue;
                }

                try
                {
                    row.Expression = (await _expressionsBuilder.Build(root, formConfigDto.StrategyKey, fieldMap)).Expression;
                }
                catch (RuleValidationException ex)
                {
                    row.Errors.Add(ex.Message);
                }
            }

            // ---- Rows that update an existing rule: load them all at once (also proves they exist). ----
            var rulesToUpdate = new Dictionary<long, Rule>();
            var idsToUpdate = rows.Where(x => x.ExistingId.HasValue && x.Errors.Count == 0)
                .Select(x => x.ExistingId!.Value).Distinct().ToList();
            foreach (var chunk in idsToUpdate.Chunk(1000))
            {
                var ids = chunk;
                var found = await AsyncExecuter.ToListAsync((await Repository.GetQueryableAsync()).Where(x => ids.Contains(x.Id)));
                foreach (var rule in found) rulesToUpdate[rule.Id] = rule;
            }

            foreach (var row in rows.Where(x => x.ExistingId.HasValue && x.Errors.Count == 0))
            {
                if (!rulesToUpdate.TryGetValue(row.ExistingId!.Value, out var target))
                    row.Errors.Add($"Rule with ID '{row.ExistingId}' was not found.");
                else if (target.WorkflowId != workflowId)
                    row.Errors.Add($"Rule with ID '{row.ExistingId}' does not belong to this workflow.");
            }

            // ---- Any problem: give the user their own sheet back, annotated. NOTHING has been saved. ----
            if (rows.Any(x => x.Errors.Count > 0))
            {
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                return BuildErrorWorkbook(wb, ws, lastRow, rows.Where(x => x.Errors.Count > 0).ToList(), workflow.Name);
            }

            // ---- Phase 2: everything is valid; save. One flush = one transaction. ----
            var newRows = rows.Where(x => !x.ExistingId.HasValue).ToList();
            var updateRows = rows.Where(x => x.ExistingId.HasValue).ToList();

            if (newRows.Count > 0)
                await SaveBulkRulesAsync(workflow, newRows);

            if (updateRows.Count > 0)
                await UpdateBulkRulesAsync(updateRows, rulesToUpdate);

            await CurrentUnitOfWork!.SaveChangesAsync();
            await EngineManager.ReloadAfterCommitAsync();   // once per unit of work, however many rules changed

            return null;
        }

        private async Task SaveBulkRulesAsync(WorkFlow workflow, List<UploadRow> rows)
        {
            // Names continue above every number already used in this workflow. (Numbering from the batch alone
            // duplicated existing names, and the rules engine cannot compile a batch that contains duplicate names.)
            var existingNames = await AsyncExecuter.ToListAsync(
                (await Repository.GetQueryableAsync()).Where(x => x.WorkflowId == workflow.Id).Select(x => x.Name));
            var names = new RuleNameAllocator(workflow.Name, existingNames);

            var rules = new List<Rule>(rows.Count);
            foreach (var row in rows)
            {
                var rule = new Rule(workflow.Id, row.Expression!, workflow.ViolationMessage);

                if (row.IsActive) rule.Activate(); else rule.Deactivate();

                rule.GenerateUniqueNameAsync(names.Next() - 1, workflow.Name);   // produces "{workflow}-Rule-{n}"
                rule.AddRuleAuditLog(RuleAuditLog.Create(rule.Id, rule, rule.ViolationType, rule.ViolationMessage, rule.IsActive, row.Expression!));
                rules.Add(rule);
            }

            await _ruleRepository.InsertManyAsync(rules);
        }

        private async Task UpdateBulkRulesAsync(List<UploadRow> rows, Dictionary<long, Rule> loadedRules)
        {
            var changed = new List<Rule>(rows.Count);
            foreach (var row in rows)
            {
                var rule = loadedRules[row.ExistingId!.Value];

                // Re-uploading an unchanged sheet must not rewrite thousands of rules and audit rows.
                if (rule.Expression == row.Expression && rule.IsActive == row.IsActive)
                    continue;

                // The sheet carries the expression and the active flag only: keep the rule's own violation
                // message and type (the previous code overwrote both with null/default).
                rule.UpdateExpression(row.Expression!);
                if (row.IsActive) rule.Activate(); else rule.Deactivate();

                rule.AddRuleAuditLog(RuleAuditLog.Create(rule.Id, rule, rule.ViolationType, rule.ViolationMessage, rule.IsActive, row.Expression!));
                changed.Add(rule);
            }

            if (changed.Count > 0)
                await _ruleRepository.UpdateManyAsync(changed);
        }

        /// <summary>Which of these codes exist? Projects only the code column, in chunks that stay under SQL parameter limits.</summary>
        private async Task<HashSet<string>> LoadExistingCodesAsync(CodeKind kind, IReadOnlyCollection<string> codes)
        {
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var chunk in codes.Chunk(1000))
            {
                var list = chunk;
                IQueryable<string> query = kind switch
                {
                    CodeKind.Activity => (await _activitySubTypeRepository.GetQueryableAsync())
                        .Where(x => list.Contains(x.iqvia_code)).Select(x => x.iqvia_code),
                    CodeKind.Speciality => (await _specialitiesRepository.GetQueryableAsync())
                        .Where(x => list.Contains(x.SpecialityCode)).Select(x => x.SpecialityCode),
                    CodeKind.GenericGroup => (await _genericDrugsRepository.GetQueryableAsync())
                        .Where(x => list.Contains(x.GenericCode)).Select(x => x.GenericCode),
                    CodeKind.Diagnosis => (await _diagnosisRepository.GetQueryableAsync())
                        .Where(x => list.Contains(x.iqvia_code)).Select(x => x.iqvia_code),
                    CodeKind.DiagnosisGroup => (await _morbidityGroupRepository.GetQueryableAsync())
                        .Where(x => list.Contains(x.MorbidityGroupSystemCode)).Select(x => x.MorbidityGroupSystemCode),
                    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
                };

                foreach (var code in await AsyncExecuter.ToListAsync(query))
                {
                    if (!string.IsNullOrWhiteSpace(code))
                        existing.Add(code);
                }
            }

            return existing;
        }

        private static CodeKind? GetCodeKind(string key)
        {
            if (string.Equals(key, "Activity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "Activities", StringComparison.OrdinalIgnoreCase)) return CodeKind.Activity;
            if (string.Equals(key, "GenericGroup", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "GenericsGroups", StringComparison.OrdinalIgnoreCase)) return CodeKind.GenericGroup;
            if (string.Equals(key, "Diagnoses", StringComparison.OrdinalIgnoreCase)) return CodeKind.Diagnosis;
            if (string.Equals(key, "DiagnosisGroupCodes", StringComparison.OrdinalIgnoreCase)) return CodeKind.DiagnosisGroup;
            if (string.Equals(key, "SpecialitiesCode", StringComparison.OrdinalIgnoreCase)) return CodeKind.Speciality;
            return null;
        }

        private static string MissingCodeMessage(CodeKind kind, string code) => kind switch
        {
            CodeKind.Activity => $"Activity '{code}' was not found in the database.",
            CodeKind.Speciality => $"Speciality '{code}' was not found in the database.",
            CodeKind.GenericGroup => $"Generic Group '{code}' was not found in the database.",
            CodeKind.Diagnosis => $"Diagnoses '{code}' was not found in the database.",
            CodeKind.DiagnosisGroup => $"Diagnosis Group Codes '{code}' was not found in the database.",
            _ => $"Code '{code}' was not found in the database."
        };

        /// <summary>A header column that maps to a rule field, resolved once per sheet.</summary>
        private sealed record SheetColumn(
            int Column,
            string Key,
            int Occurrence,
            RuleFormConfigField Config,
            RuleField Field,
            int OperatorColumn,
            RuleOperator? DefaultOperator,
            CodeKind? CodeKind);

        /// <summary>One data row of the uploaded sheet, with everything that is wrong with it.</summary>
        private sealed class UploadRow
        {
            public UploadRow(int rowNumber) => RowNumber = rowNumber;

            public int RowNumber { get; }
            public long? ExistingId { get; set; }
            public bool IsActive { get; set; } = true;
            public List<LayoutCondition> Conditions { get; } = new();
            public string? Expression { get; set; }
            public List<string> Errors { get; } = new();
        }

        /// <summary>Builds a new workbook from scratch with the original headers and only the rows that contain errors.</summary>
        private static IRemoteStreamContent BuildErrorWorkbook(
            XLWorkbook wb, IXLWorksheet ws, int lastRow, List<UploadRow> rows, string? originalFileName)
        {
            var lastColumn = ws.LastColumnUsed()?.ColumnNumber() ?? 1;
            var errorRows = rows.Where(x => x.Errors.Count > 0)
                .OrderBy(x => x.RowNumber)
                .ToList();

            if (errorRows.Count == 0)
            {
                var output = new MemoryStream();
                var emptyWorkbook = new XLWorkbook();
                var emptySheet = emptyWorkbook.Worksheets.Add("Rules_Import");
                emptySheet.Cell(1, 1).Value = ErrorsHeader;
                emptyWorkbook.SaveAs(output);
                output.Position = 0;

                var baseName = Path.GetFileNameWithoutExtension(originalFileName);
                if (string.IsNullOrWhiteSpace(baseName)) baseName = "Rules_Import";

                return new RemoteStreamContent(output, $"{baseName}_Errors.xlsx", ExcelContentType);
            }

            var outputWorkbook = new XLWorkbook();
            var outputSheet = outputWorkbook.Worksheets.Add("Rules_Import");

            for (var c = 1; c <= lastColumn; c++)
            {
                var sourceCell = ws.Cell(1, c);
                var targetCell = outputSheet.Cell(1, c);
                targetCell.Value = sourceCell.Value;
                targetCell.Style = sourceCell.Style;
            }

            var errorColumn = lastColumn + 1;
            outputSheet.Cell(1, errorColumn).Value = ErrorsHeader;
            outputSheet.Cell(1, errorColumn).Style = ws.Cell(1, lastColumn).Style;
            outputSheet.Column(errorColumn).Width = 60;

            var outputRow = 2;
            foreach (var row in errorRows)
            {
                for (var c = 1; c <= lastColumn; c++)
                {
                    var sourceCell = ws.Cell(row.RowNumber, c);
                    outputSheet.Cell(outputRow, c).Value = sourceCell.Value;
                }

                var errorCell = outputSheet.Cell(outputRow, errorColumn);
                errorCell.Value = string.Join(", ", row.Errors);
                errorCell.Style.Font.FontColor = XLColor.Red;
                errorCell.Style.Alignment.WrapText = true;

                outputRow++;
            }

            var stream = new MemoryStream();
            outputWorkbook.SaveAs(stream);
            stream.Position = 0;

            var baseName2 = Path.GetFileNameWithoutExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(baseName2)) baseName2 = "Rules_Import";

            return new RemoteStreamContent(stream, $"{baseName2}_Errors.xlsx", ExcelContentType);
        }
    }
}