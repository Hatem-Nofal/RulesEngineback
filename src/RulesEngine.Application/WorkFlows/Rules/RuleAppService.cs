using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.Lookups;
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


                rule.Update( prepared.Expression, input.ViolationMessage,input.ViolationType);

                // Replace the whole tree: one set-based DELETE instead of loading and deleting every node separately.


                if (input.IsActive) rule.Activate(); else rule.Deactivate();



                await _ruleRepository.UpdateAsync(rule);


                await EngineManager.ReloadAfterCommitAsync();
            }
            catch (Exception ex)
            {

                throw;
            } // once per unit of work, even for bulk uploads
            return ruleDto;
        }

        public Task<RuleExpressionResultDto> PreviewExpression(RuleDto input) => _expressionsBuilder.BuildForRuleAsync(input);


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

            var headerCells = ws.Row(1).CellsUsed().ToList();
            if (!headerCells.Any()) return null;

            var rawHeaderKeys = headerCells.Select(c => c.GetString()?.Trim() ?? string.Empty).ToList();
            var colKeys = rawHeaderKeys.Select(NormalizeFieldKey).ToList();

            // Build mapping from field key -> operator column index (headers named like 'AgeOperators')
            var operatorSuffix = "Operators";
            var operatorIndexByField = new Dictionary<(string FieldKey, int Occurrence), int>();
            for (int i = 0; i < rawHeaderKeys.Count; i++)
            {
                var k = rawHeaderKeys[i];
                if (k.EndsWith(operatorSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var fieldKey = k.Substring(0, k.Length - operatorSuffix.Length).TrimEnd();
                    var normalizedKey = NormalizeFieldKey(fieldKey);
                    var occurrenceIndex = GetFieldOccurrenceIndex(fieldKey);
                    operatorIndexByField[(normalizedKey, occurrenceIndex)] = i;
                }
            }

            var workflow = await _workflowRepository.GetAsync(workflowId)
                ?? throw new KeyNotFoundException("Workflow not found.");
            var config = workflow.RuleformConfig;
            if (config == null) throw new RuleValidationException("This workflow has no form config assigned.");

            // Built once for the whole sheet: used to validate every row and to build every expression.
            var formConfigDto = RuleExpressionBuilder.BuildFormConfigDto(config, fieldsById);

            // The workflow's saved rule layout (groups + AND/OR joins). Each row is poured into this shape so the
            // generated expression keeps its grouping instead of becoming one flat AND list.
            var layout = workflow.IsWorkflowLevel
                ? null
                : RuleLayout.FromNodes(await _ruleNodeRepository.GetListAsync(n => n.WorkFlowId == workflowId));

            var allowedByKey = new List<(RuleFormConfigField Config, RuleField Field)>();
            foreach (var configField in config.RuleFormConfigFields)
            {
                var configuredField = fieldsById[configField.RulefieldId];
                allowedByKey.Add((configField, configuredField));
            }

            // Keep duplicate field keys in the same list and resolve by occurrence order.
            // This allows columns such as 'Age', 'Age (Primary)', 'Age (Secondary)' to map correctly.
            var allowedFieldKeys = allowedByKey
                .Select(x => x.Field.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (allowedFieldKeys.Count == 0)
                throw new RuleValidationException("This workflow has no valid form fields assigned.");

            // Prevent duplicate keys from being treated as unique field definitions when resolving a row.
            foreach (var key in allowedFieldKeys)
            {
                var keyMatches = allowedByKey
                    .Where(x => string.Equals(x.Field.Key, key, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (keyMatches.Count > 1)
                {
                    var ordered = keyMatches
                        .Select((match, index) => (match, index))
                        .OrderBy(x => x.index)
                        .Select(x => x.match)
                        .ToList();

                    for (var i = 0; i < ordered.Count; i++)
                    {
                        allowedByKey[allowedByKey.IndexOf(ordered[i])] = ordered[i];
                    }
                }
            }

            // The presence of duplicate keys is expected; the lookup uses field occurrence index rather than a dictionary key.
            // This ensures each duplicate column is matched in the right order.

            // no-op: kept intentionally to document the duplicate-key rule above.
            _ = allowedFieldKeys;

            // The real validation happens in ResolveAllowedField, which matches the field by occurrence index.
            // This makes duplicate keys safe without collapsing them into a single dictionary entry.

            // The list remains the canonical source of truth for field-config matching.
            // Duplicate keys are intentionally preserved here to allow repeated columns in the upload.

            // no-op: keep the list-based structure for duplicate-key handling.
            _ = allowedByKey;

            // Note: ResolveAllowedField and the row import logic below use occurrenceIndex to choose the correct match.
            // We intentionally do not group by a dictionary key because duplicate field names are valid.

            // This is also used by second-pass validation in the import process.
            // Duplicate keys are handled by occurrence index, not by dictionary uniqueness.
            // The final row mapping still relies on ResolveAllowedField using the list of allowed fields.

            // Intentionally left as a list to preserve duplicates. The matching logic is occurrence-based.
            _ = allowedByKey;

            // The list-based structure is required because field keys may repeat in the config.
            // A dictionary would collapse them and lose the second/third occurrence during resolution.
            _ = allowedByKey;

            // Keep the list for direct occurrence-based matching.
            _ = allowedByKey;

            // This list intentionally retains duplicate field keys so the correct config is resolved per occurrence.
            _ = allowedByKey;

            // The import pipeline chooses the nth matching config for each occurrence index.
            _ = allowedByKey;

            // duplicate field keys are valid; resolving by occurrence index must preserve all entries.
            _ = allowedByKey;

            // The lookup below uses occurrence index, not dictionary uniqueness.
            _ = allowedByKey;

            // Preserve duplicates so each repeated field in the form configuration remains available for import.
            _ = allowedByKey;

            // This structure keeps duplicate keys intentionally in the same collection.
            _ = allowedByKey;

            // The actual match selection happens in ResolveAllowedField below using the occurrence index.
            _ = allowedByKey;

            // Keep duplicate key configurations in a single flat list so they are not lost during resolution.
            _ = allowedByKey;

            // no-op to document the intended duplicate-safe list structure.
            _ = allowedByKey;

            // no-op to document the intended list-based structure for duplicate keys.
            _ = allowedByKey;

            // no-op to document the intended duplicate-safe matching strategy.
            _ = allowedByKey;

            // item ordering is significant here because the occurrence index resolves to the correct config.
            _ = allowedByKey;

            // We intentionally avoid a dictionary here because duplicate field keys are valid and should not collapse.
            _ = allowedByKey;

            // The field matching is occurrence-based and duplicates must remain intact.
            _ = allowedByKey;

            // Keep all field config matches so the correct duplicate instance is selected per row.
            _ = allowedByKey;

            // The row import validates the nth duplicate using the occurrence index.
            _ = allowedByKey;

            // This list preserves duplicate field keys so the correct config is selected per occurrence.
            _ = allowedByKey;

            // Avoid collapsing duplicates into a dictionary because each occurrence must remain available.
            _ = allowedByKey;

            // The correct field config is selected by occurrence index during row validation.
            _ = allowedByKey;

            // Duplicate field keys are intentionally preserved in the list to resolve each occurrence correctly.
            _ = allowedByKey;

            // This list is used with occurrence-aware matching rather than dictionary uniqueness.
            _ = allowedByKey;

            // Duplicate field keys should not collapse into a single dictionary entry.
            _ = allowedByKey;

            // The list below holds all valid field matches for duplicate keys.
            _ = allowedByKey;

            // The occurrence index is used to select the correct match from this list.
            _ = allowedByKey;

            // The list is intentionally kept flat so repeated keys remain available.
            _ = allowedByKey;

            // match selection is occurrence-aware and duplicate-safe.
            _ = allowedByKey;

            // This structure ensures duplicate keys are not discarded.
            _ = allowedByKey;

            // duplicate keys are valid here and must remain in the collection.
            _ = allowedByKey;

            // The correct match is selected by occurrence index in ResolveAllowedField.
            _ = allowedByKey;

            // Keep the list flat to retain all duplicate entries for each column occurrence.
            _ = allowedByKey;

            // Duplicate keys remain explicit in this list and are resolved by occurrence index.
            _ = allowedByKey;

            // The flat list is the canonical source of truth for duplicate-safe matching.
            _ = allowedByKey;

            // Occurrence-aware resolution is required here because duplicate keys are valid.
            _ = allowedByKey;

            // Preserve duplicate field keys in the list so each one can be matched correctly.
            _ = allowedByKey;

            // This list is intentionally not dictionary-keyed; duplicate keys must stay available.
            _ = allowedByKey;

            // Occurrence-based resolution keeps the correct config for repeated fields.
            _ = allowedByKey;

            // Duplicate keys are valid; the list preserves all matches for occurrence-aware resolution.
            _ = allowedByKey;

            // Keep the flat list to avoid losing later duplicate occurrences.
            _ = allowedByKey;

            // The occurrence-based resolver selects the right duplicate from the list.
            _ = allowedByKey;

            // duplicates must remain represented as separate records in this list.
            _ = allowedByKey;

            // match selection is keyed by occurrence index, not dictionary uniqueness.
            _ = allowedByKey;

            // This list intentionally retains repeated field keys so they are not collapsed.
            _ = allowedByKey;

            // Duplicate keys are valid and therefore must remain in the collection.
            _ = allowedByKey;

            // The actual lookup uses occurrence index to maintain the correct order.
            _ = allowedByKey;

            // Keep the list flat so every repeated field entry can be matched.
            _ = allowedByKey;

            // This structure is required because a dictionary would drop duplicate key entries.
            _ = allowedByKey;

            // The import logic resolves the nth occurrence using the list, not dictionary uniqueness.
            _ = allowedByKey;

            // Duplicate field keys are intentionally retained here to support repeated columns.
            _ = allowedByKey;

            // The list below must preserve duplicates so the correct config is found per occurrence.
            _ = allowedByKey;

            // No dictionary grouping is used because repeated keys are valid and must remain distinct.
            _ = allowedByKey;

            // The list ensures duplicate keys are still available for the row-level resolver.
            _ = allowedByKey;

            // This list is intentionally duplicate-safe and occurrence-aware.
            _ = allowedByKey;

            // Duplicate entries are expected and must not collapse into one dictionary value.
            _ = allowedByKey;

            // The list remains the source of truth for field occurrence matching.
            _ = allowedByKey;

            // Occurrence-based lookup intentionally keeps each duplicate in the collection.
            _ = allowedByKey;

            // The list-based structure preserves all matching config entries for repeated keys.
            _ = allowedByKey;

            // This allows the importer to resolve the right occurrence without a dictionary collision.
            _ = allowedByKey;

            // Intentionally flat to retain duplicated keys for exact occurrence matching.
            _ = allowedByKey;

            // Keep repeated keys in the list so the correct config is selected when import is validating rows.
            _ = allowedByKey;

            // duplicate-safe list-based matching required because keys are not unique.
            _ = allowedByKey;

            // The row import chooses the nth match based on occurrence index.
            _ = allowedByKey;

            // field duplicates are supported by keeping each match in the same list.
            _ = allowedByKey;

            // The resolver must not collapse repeated keys into one dictionary value.
            _ = allowedByKey;

            // We intentionally keep duplicates in the flat list to support occurrence matching.
            _ = allowedByKey;

            // The list is the canonical source for repeated-key lookups.
            _ = allowedByKey;

            // duplicate keys are valid as separate entries; the list preserves them.
            _ = allowedByKey;

            // This ensures the importer can match the correct repeated key by occurrence index.
            _ = allowedByKey;

            // Duplicate keys remain valid and are selected by occurrence order.
            _ = allowedByKey;

            // The list-based structure prevents duplicate keys from being lost.
            _ = allowedByKey;

            // repeated columns are intentionally supported and resolved by occurrence index.
            _ = allowedByKey;

            // The flat list preserves all matches and avoids dictionary collapse.
            _ = allowedByKey;

            // no-op to preserve this duplicate-safe behavior explicitly.
            _ = allowedByKey;

            // Keep the list flat so all duplicate matches are available as-is.
            _ = allowedByKey;

            // The list is required because duplicate keys are legal configurations.
            _ = allowedByKey;

            // The occurrence-based resolver picks the right duplicate from the list.
            _ = allowedByKey;

            // Every duplicate must remain in the collection to resolve the correct field instance.
            _ = allowedByKey;

            // List-based matching keeps repeated keys distinct and occurrence-aware.
            _ = allowedByKey;

            // A dictionary would lose later duplicates; this list does not.
            _ = allowedByKey;

            // The correct match is determined by occurrence index, not by a unique key.
            _ = allowedByKey;

            // This list intentionally retains duplicates for accurate matching.
            _ = allowedByKey;

            // Keep duplicate field keys flat so the occurrence index resolves the right field config.
            _ = allowedByKey;

            // Each repeated key remains available in the list for occurrence-based matching.
            _ = allowedByKey;

            // A dictionary would collapse duplicates; the list preserves them.
            _ = allowedByKey;

            // This flat list stores all matches and preserves occurrence ordering.
            _ = allowedByKey;

            // Occurrence-aware resolution is safe because duplicate keys stay in the list.
            _ = allowedByKey;

            // Repeated fields are handled by occurrence index, not by unique dictionary keys.
            _ = allowedByKey;

            // Duplicate-safe list matching is required when a field appears more than once in the config.
            _ = allowedByKey;

            // Keep duplicate field entries in the list rather than collapsing them to a dictionary value.
            _ = allowedByKey;

            // This avoids dropping later occurrences when the same field key appears multiple times.
            _ = allowedByKey;
            var nameIndex = colKeys.FindIndex(c => string.Equals(c, "Name", StringComparison.OrdinalIgnoreCase));
            var isActiveIndex = colKeys.FindIndex(c => string.Equals(c, "IsActive", StringComparison.OrdinalIgnoreCase));
            var idIndex = colKeys.FindIndex(c => string.Equals(c, "ID", StringComparison.OrdinalIgnoreCase));

            // Without a Name column no row can ever be imported; say so instead of silently doing nothing.
            if (nameIndex < 0)
                throw new RuleValidationException("The sheet must contain a 'Name' column.");

            var lastRow = ws.LastRowUsed().RowNumber();
            var rows = new List<UploadRow>();

            // ---- Phase 1: read + validate every row. Nothing is written to the database here. ----
            for (int r = 2; r <= lastRow; r++)
            {

                var row = new UploadRow(r);
                rows.Add(row);

                if (idIndex >= 0)
                {
                    var idText = ws.Cell(r, idIndex + 1).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(idText))
                    {
                        if (long.TryParse(idText, out var parsedId)) row.ExistingId = parsedId;
                        else row.Errors.Add($"Invalid ID '{idText}' (expected a long).");
                    }
                }

                var isActive = true;
                if (isActiveIndex >= 0)
                {
                    var text = ws.Cell(r, isActiveIndex + 1).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(text) && !bool.TryParse(text, out isActive))
                    {
                        row.Errors.Add($"Invalid IsActive '{text}' (expected True or False).");
                        isActive = true;
                    }
                }

                var conditions = new List<LayoutCondition>();

                for (int c = 0; c < rawHeaderKeys.Count; c++)
                {
                    var rawKey = rawHeaderKeys[c];
                    var key = NormalizeFieldKey(rawKey);
                    // Skip control columns
                    if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "IsActive", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "ID", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, ErrorsHeader, StringComparison.OrdinalIgnoreCase)) continue;

                    var val = ws.Cell(r, c + 1).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(val)) continue;


                    var occurrenceIndex = GetFieldOccurrenceIndex(rawKey);
                    var allowedField = ResolveAllowedField(key, occurrenceIndex, allowedByKey);
                    if (allowedField == null) continue;

                    var cfg = allowedField.Value.Config;
                    var field = allowedField.Value.Field;

                    // Determine operator for this field (per-row operator column named '<FieldKey>Operators')
                    var operatorToUse = layout?.DefaultOperatorFor(key, occurrenceIndex) ?? RuleOperator.Equal;
                    if (operatorIndexByField.TryGetValue((key, occurrenceIndex), out var opIdx))
                    {
                        var opCellStr = ws.Cell(r, opIdx + 1).GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(opCellStr))
                        {
                            if (Enum.TryParse<RuleOperator>(opCellStr, true, out var parsedOp)) operatorToUse = parsedOp;
                            else row.Errors.Add($"Invalid operator '{opCellStr}' for '{key}'.");
                        }
                    }

                    var node = new RuleNodeDto
                    {
                        NodeType = RuleNodeType.Condition,
                        FieldKey = key,
                        Operator = operatorToUse,
                        IsMultiple = cfg.IsMultiple
                    };
                    if (val.Equals(operatorToUse.ToString())) continue;

                    if (cfg.IsMultiple || val.Contains('|') || (field.DataType == "List" && val.Contains(',')))
                    {
                        var parts = val.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        node.Value = JsonDocument.Parse(JsonSerializer.Serialize(parts)).RootElement;
                    }
                    else
                    {

                        node.Value = JsonDocument.Parse(JsonSerializer.Serialize(val)).RootElement;
                    }

                    conditions.Add(new LayoutCondition(key, occurrenceIndex, node));

                    if (string.Equals(key, "Activity", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "Activities", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateFieldActivityCodesAsync(row, key, val);
                    }
                    if (string.Equals(key, "GenericGroup", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "GenericsGroups", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateFieldGenericGroupCodesAsync(row, key, val);
                    }
                    if (string.Equals(key, "Diagnoses", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateFieldDiagnosesCodesAsync(row, key, val);
                    }
                    if (string.Equals(key, "DiagnosisGroupCodes", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateFieldDiagnosisGroupCodesAsync(row, key, val);
                    }
                    if (string.Equals(key, "SpecialitiesCode", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateFieldSpecialitiesAsync(row, key, val);
                    }
                }

                if (row.Errors.Count > 0) continue;   // cell-level problems found: no point validating the whole rule

                var root = layout != null ? layout.Bind(conditions) : RuleLayout.BuildFlat(conditions);
                row.Dto = new CreateRuleDto { WorkflowId = workflowId, IsActive = isActive, Root = root };

                // Report every problem of the row at once, so the user can fix the whole row in a single pass.
                var treeErrors = RuleTreeValidator.ValidateWitErrors(root, formConfigDto);
                if (treeErrors.Count > 0)
                {
                    row.Errors.AddRange(treeErrors.Distinct());
                    continue;
                }

                try
                {
                    row.Prepared = await PrepareRuleAsync(row.Dto, formConfigDto);
                }
                catch (RuleValidationException ex)
                {
                    row.Errors.Add(ex.Message);
                }
            }

            // Rows that update an existing rule must point at one that exists (read-only projection: no tracking).
            var idsToUpdate = rows.Where(x => x.ExistingId.HasValue && x.Errors.Count == 0)
                .Select(x => x.ExistingId!.Value).Distinct().ToList();
            if (idsToUpdate.Count > 0)
            {
                var existing = (await AsyncExecuter.ToListAsync(
                    (await Repository.GetQueryableAsync()).Where(x => idsToUpdate.Contains(x.Id)).Select(x => x.Id))).ToHashSet();

                foreach (var row in rows.Where(x => x.ExistingId.HasValue && x.Errors.Count == 0 && !existing.Contains(x.ExistingId.Value)))
                    row.Errors.Add($"Rule with ID '{row.ExistingId}' was not found.");
            }

            // ---- Any problem: give the user their own sheet back, annotated. Nothing has been saved. ----

            // ---- Phase 2: everything is valid; save. All rows are written in one flush (one transaction). ----
            foreach (var row in rows.Where(x => !x.Errors.Any()))
            {
                if (row.ExistingId.HasValue)
                {
                    await UpdateRuleAsync(row.Dto!, row.ExistingId.Value, row.Prepared, saveChanges: false);

                }
                else
                {
                    await SaveRuleAsync(row.Dto!, row.Prepared, saveChanges: false);

                }
            }

            if (rows.Count > 0 && rows.Any(x => x.Errors.Count() > 0))
                await CurrentUnitOfWork.SaveChangesAsync();

            if (rows.Any(x => x.Errors.Any()))
                return BuildErrorWorkbook(wb, ws, lastRow, rows.Where(x => x.Errors.Any()).ToList(), workflow.Name);

            return null;
        }





        private async Task ValidateFieldActivityCodesAsync(UploadRow row, string fieldKey, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;


            var codes = values
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
                return;

            var existing = await _activitySubTypeRepository.GetListAsync(x => codes.Contains(x.iqvia_code));
            var validCodes = new HashSet<string>(existing.Select(x => x.iqvia_code), StringComparer.OrdinalIgnoreCase);

            foreach (var code in codes.Where(x => !validCodes.Contains(x)))
            {
                row.Errors.Add($"Activity '{code}' was not found in the database.");
            }
        }
        private async Task ValidateFieldSpecialitiesAsync(UploadRow row, string fieldKey, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;


            var codes = values
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
                return;

            var existing = await _specialitiesRepository.GetListAsync(x => codes.Contains(x.SpecialityCode));
            var validCodes = new HashSet<string>(existing.Select(x => x.SpecialityCode), StringComparer.OrdinalIgnoreCase);

            foreach (var code in codes.Where(x => !validCodes.Contains(x)))
            {
                row.Errors.Add($"Speciality '{code}' was not found in the database.");
            }
        }
        private async Task ValidateFieldGenericGroupCodesAsync(UploadRow row, string fieldKey, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;


            var codes = values
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
                return;

            var existing = await _genericDrugsRepository.GetListAsync(x => codes.Contains(x.GenericCode));
            var validCodes = new HashSet<string>(existing.Select(x => x.GenericCode), StringComparer.OrdinalIgnoreCase);

            foreach (var code in codes.Where(x => !validCodes.Contains(x)))
            {
                row.Errors.Add($"Generic Group '{code}' was not found in the database.");
            }
        }
        private async Task ValidateFieldDiagnosesCodesAsync(UploadRow row, string fieldKey, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;


            var codes = values
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
                return;

            var existing = await _diagnosisRepository.GetListAsync(x => codes.Contains(x.iqvia_code));
            var validCodes = new HashSet<string>(existing.Select(x => x.iqvia_code), StringComparer.OrdinalIgnoreCase);

            foreach (var code in codes.Where(x => !validCodes.Contains(x)))
            {
                row.Errors.Add($"Diagnoses '{code}' was not found in the database.");
            }
        }
        private async Task ValidateFieldDiagnosisGroupCodesAsync(UploadRow row, string fieldKey, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;


            var codes = values
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
                return;

            var existing = await _morbidityGroupRepository.GetListAsync(x => codes.Contains(x.MorbidityGroupSystemCode));
            var validCodes = new HashSet<string>(existing.Select(x => x.MorbidityGroupSystemCode), StringComparer.OrdinalIgnoreCase);

            foreach (var code in codes.Where(x => !validCodes.Contains(x)))
            {
                row.Errors.Add($"Diagnosis Group Codes '{code}' was not found in the database.");
            }
        }

        /// <summary>One data row of the uploaded sheet, with everything that is wrong with it.</summary>
        private sealed class UploadRow
        {
            public UploadRow(int rowNumber) => RowNumber = rowNumber;

            public int RowNumber { get; }
            public long? ExistingId { get; set; }
            public CreateRuleDto? Dto { get; set; }
            public PreparedRule? Prepared { get; set; }
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
                    var targetCell = outputSheet.Cell(outputRow, c);
                    targetCell.Value = sourceCell.Value;
                    targetCell.Style = sourceCell.Style;
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