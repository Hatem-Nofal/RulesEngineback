using ClosedXML.Excel;
using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.IntegrationsNew.RulesEngineManagers;
using RulesEngine.Lookups;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.Helpers;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.WorkFlows.Rules
{

    public class RuleAppService : CrudAppService<Rule, RuleDto, RuleListItemDto, Guid, PagedAndSortedResultRequestDto, CreateRuleDto, CreateRuleDto>, IRuleAppService
    {
        private readonly IRepository<RuleNode, Guid> _ruleNodeRepository;
        private readonly IWorkFlowRepository _workflowRepository;
        private readonly IRepository<RuleField, Guid> _ruleFieldRepository;
        private readonly IRuleExpressionBuilder _expressionsBuilder;
        private readonly IRuleRepository _ruleRepository;
        private readonly IRepository<ActivitySubType> _activitySubTypeRepository;


        private IRulesEngineManager EngineManager => LazyServiceProvider.LazyGetRequiredService<IRulesEngineManager>();

        public RuleAppService(
            IRepository<Rule, Guid> repository,
            IRepository<RuleNode, Guid> ruleNodeRepository,
           IWorkFlowRepository workflowRepository,
             IRuleExpressionBuilder expressionsBuilder,
            IRuleRepository ruleRepository,
            IRepository<RuleField, Guid> ruleFieldRepository,
            IRepository<ActivitySubType> activitySubTypeRepository) : base(repository)
        {
            _ruleNodeRepository = ruleNodeRepository;
            _workflowRepository = workflowRepository;
            _expressionsBuilder = expressionsBuilder;
            _ruleRepository = ruleRepository;
            _ruleFieldRepository = ruleFieldRepository;
            _activitySubTypeRepository = activitySubTypeRepository;
        }

        public override async Task<PagedResultDto<RuleListItemDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            // Paging happens in SQL and only the four list columns are read. The previous version loaded every rule
            // (including its expression text) into memory just to take one page of it.
            var query = (await Repository.GetQueryableAsync())
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id);   // stable order, so a row can never appear on two pages

            var total = await AsyncExecuter.CountAsync(query);

            var page = await AsyncExecuter.ToListAsync(
                query.Skip(input.SkipCount)
                     .Take(input.MaxResultCount)
                     .Select(x => new { x.Id, x.Name, x.WorkflowId, x.IsActive }));

            var workflows = (await _workflowRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

            var items = page.Select(x => new RuleListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                WorkflowId = x.WorkflowId,
                WorkflowName = workflows.GetValueOrDefault(x.WorkflowId),
                IsActive = x.IsActive
            }).ToList();

            return new PagedResultDto<RuleListItemDto>(total, items);
        }

        public override async Task<RuleDto> GetAsync(Guid id)
        {
            var rule = await _ruleRepository.GetAsync(id)
                       ?? throw new KeyNotFoundException("Rule not found.");


            return new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                WorkflowId = rule.WorkflowId,
                IsActive = rule.IsActive,
                Root = !rule.Workflow.HasCustomeRule ? await RuleTreeMapper.ToTree(rule.Nodes.OrderBy(x => x.Order).ToList(), rule.Workflow.RuleformConfig.RuleFormConfigFields.ToList(), _activitySubTypeRepository) : new RuleNodeDto(),
                Expression = rule.Expression,
                WorkflowHasCustomeRule = rule.Workflow.HasCustomeRule,
                ViolationMessage=rule.ViolationMessage,
               ViolationType=rule.ViolationType 
            };
        }

        public override async Task<RuleDto> CreateAsync(CreateRuleDto input)
        {
            return await SaveRuleAsync(input, null);
        }

        public override async Task<RuleDto> UpdateAsync(Guid id, CreateRuleDto input)
        {
            return await UpdarteRuleAsync(input, id);
        }

        public override async Task DeleteAsync(Guid id)
        {
            var rule = await Repository.GetAsync(x => x.Id == id);

            // One set-based DELETE for all nodes instead of loading them and deleting them one by one.
            await _ruleNodeRepository.DeleteDirectAsync(n => n.RuleId == id);

            await Repository.HardDeleteAsync(rule);
            await EngineManager.ReloadAfterCommitAsync();
        }

        /// <summary>Validated rule definition: everything needed to save, computed without any database write.</summary>
        private sealed record PreparedRule(RuleDto Dto, string Expression);

        /// <summary>
        /// Loads the workflow's form config as a DTO: one query for the workflow (with its config and config fields)
        /// and one for the field catalog. Code that handles many rules (bulk import) calls this once and reuses the result.
        /// </summary>
        private async Task<RuleFormConfigDto> LoadFormConfigAsync(Guid workflowId)
        {
            var workflow = await _workflowRepository.GetAsync(workflowId)
                           ?? throw new RuleValidationException("Workflow not found.");
            var config = workflow.RuleformConfig
                         ?? throw new RuleValidationException("This workflow has no form config assigned.");

            var ruleFields = await _ruleFieldRepository.GetListAsync();
            return BuildFormConfigDto(config, ruleFields.ToDictionary(x => x.Id));
        }

        /// <summary>Pure mapping; every field is found through a dictionary instead of a linear scan per property.</summary>
        private static RuleFormConfigDto BuildFormConfigDto(RuleFormConfig config, IReadOnlyDictionary<Guid, RuleField> fieldsById)
        {
            var fields = new List<RuleFormFieldDto>(config.RuleFormConfigFields.Count);
            foreach (var configField in config.RuleFormConfigFields)
            {
                if (!fieldsById.TryGetValue(configField.RulefieldId, out var field))
                    throw new RuleValidationException("The workflow's form config references a field that no longer exists.");

                fields.Add(new RuleFormFieldDto
                {
                    Id = configField.RulefieldId,
                    Key = field.Key,
                    DataType = field.DataType,
                    AllowedOperators = ListExtensions.DeserializeIntList(configField.AllowedOperatorsJson),
                    Label = field.Label,
                    Options = ListExtensions.DeserializeStringList(field.OptionsJson),
                    IsMultiple = configField.IsMultiple
                });
            }

            return new RuleFormConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                AllowNestedGroups = config.AllowNestedGroups,
                StrategyKey = config.StrategyKey,
                Fields = fields
            };
        }

        /// <summary>
        /// Validates the rule tree against the workflow's form config and builds the expression. Never writes.
        /// Pass <paramref name="formConfig"/> when preparing many rules of the same workflow so it is loaded only once.
        /// </summary>
        private async Task<PreparedRule> PrepareRuleAsync(CreateRuleDto input, RuleFormConfigDto? formConfig = null)
        {


            formConfig ??= await LoadFormConfigAsync(input.WorkflowId);

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

            var ruleId = Guid.CreateVersion7();

            var rule = new Rule(ruleId, input.WorkflowId, prepared.Expression, input.ViolationMessage);

            var workflow = await _workflowRepository.GetAsync(input.WorkflowId);
            var count = await _ruleRepository.CountAsync(x => x.WorkflowId == input.WorkflowId);


            if (input.IsActive) rule.Activate(); else rule.Deactivate();

            var flatNodes = new List<RuleNode>();
            RuleTreeMapper.Flatten(input.Root, rule.Id, null, 0, flatNodes);
            rule.AddNodes(flatNodes);
            rule.GenerateUniqueNameAsync(count, workflow.Name);

            await _ruleRepository.InsertAsync(rule);


            if (rule.Nodes.Count > 0)
            {
                await _ruleNodeRepository.InsertManyAsync(rule.Nodes);
                if (saveChanges)
                    await CurrentUnitOfWork.SaveChangesAsync();
            }

            await EngineManager.ReloadAfterCommitAsync();   // once per unit of work, even for bulk uploads
            return ruleDto;
        }
        private async Task<RuleDto> UpdarteRuleAsync(CreateRuleDto input, Guid id, PreparedRule? prepared = null, bool saveChanges = true)
        {
            prepared ??= await PrepareRuleAsync(input);
            var ruleDto = prepared.Dto;

            _ruleRepository.DisableTracking();
            var rule = await _ruleRepository.GetForUpdateAsync(id);


            rule.Update(input.WorkflowId, prepared.Expression, input.ViolationMessage);

            // Replace the whole tree: one set-based DELETE instead of loading and deleting every node separately.
            await _ruleNodeRepository.DeleteDirectAsync(n => n.RuleId == rule.Id);


            if (input.IsActive) rule.Activate(); else rule.Deactivate();

            var flatNodes = new List<RuleNode>();
            RuleTreeMapper.Flatten(input.Root, rule.Id, null, 0, flatNodes);
            rule.AddNodes(flatNodes);


            await _ruleRepository.UpdateAsync(rule);

            if (rule.Nodes.Count > 0)
            {
                await _ruleNodeRepository.InsertManyAsync(rule.Nodes);
                if (saveChanges)
                    await CurrentUnitOfWork.SaveChangesAsync();
            }

            await EngineManager.ReloadAfterCommitAsync();   // once per unit of work, even for bulk uploads
            return ruleDto;
        }

        public Task<RuleExpressionResultDto> PreviewExpression(RuleDto input) => _expressionsBuilder.BuildForRuleAsync(input);


        public async Task<IRemoteStreamContent> GetRulesSampleAsync(Guid workflowId)
        {
            var workflow = await _workflowRepository.GetAsync(workflowId)
                ?? throw new KeyNotFoundException("Workflow not found.");

            var config = workflow.RuleformConfig;

            if (config == null)
                throw new RuleValidationException(
                    "This workflow has no form config assigned.");

            var configFields = config.RuleFormConfigFields
                .Where(x => x.IsDefault)
                .OrderBy(f => f.Order)
                .ToList();

            var configFieldIds = configFields
                .Select(x => x.RulefieldId)
                .ToHashSet();

            var fields = (await _ruleFieldRepository.GetListAsync(
                x => configFieldIds.Contains(x.Id)))
                .OrderBy(f => configFields.FindIndex(
                    cf => cf.RulefieldId == f.Id))
                .ToList();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("RulesSample");
            var optionsSheet = workbook.Worksheets.Add("Options");

            // Headers
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "IsActive";
            ws.Cell(1, 3).Value = "ID";

            // For each field we will have two columns: <FieldKey> and <FieldKey>Operators
            for (int i = 0; i < fields.Count; i++)
            {
                ws.Cell(1, 4 + i * 2).Value = fields[i].Key;
                ws.Cell(1, 5 + i * 2).Value = fields[i].Key + "Operators";
            }

            ws.Row(1).Style.Font.SetBold();

            // Add dropdowns for fields with OptionsJson.
            // Each field gets its own options column and named range.
            const int lastRowValidation = 1000;

            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];

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
            int operatorOptionsCol = fields.Count + 1; // after per-field options columns
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
            for (int i = 0; i < fields.Count; i++)
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

            // Populate sample row or existing rules.
            var rules = (await Repository.GetListAsync(
                r => r.WorkflowId == workflowId))
                .ToList();

            int row = 2;

            if (!rules.Any())
            {
                ws.Cell(row, 1).Value = "Sample rule";
                ws.Cell(row, 2).Value = true;

                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    int fieldCol = 4 + i * 2;
                    int operatorCol = fieldCol + 1;

                    // default operator = Equal, default value = first option (if available)
                    ws.Cell(row, operatorCol).Value = RuleOperator.Equal.ToString();

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
                var ruleIds = rules.Select(r => r.Id).ToList();

                var allNodes = await _ruleNodeRepository.GetListAsync(
                    n => ruleIds.Contains(n.RuleId));

                // First node per (rule, field key): same result as the old FirstOrDefault scan, without
                // rules x fields x nodes comparisons.
                var nodeByRuleAndField = new Dictionary<(Guid RuleId, string FieldKey), RuleNode>();
                foreach (var indexedNode in allNodes)
                {
                    if (indexedNode.FieldKey is not null)
                        nodeByRuleAndField.TryAdd((indexedNode.RuleId, indexedNode.FieldKey), indexedNode);
                }

                foreach (var rule in rules)
                {
                    ws.Cell(row, 1).Value = rule.Name;
                    ws.Cell(row, 2).Value = rule.IsActive;
                    ws.Cell(row, 3).Value = rule.Id.ToString();

                    for (int i = 0; i < fields.Count; i++)
                    {
                        var f = fields[i];
                        int fieldCol = 4 + i * 2;
                        int operatorCol = fieldCol + 1;

                        nodeByRuleAndField.TryGetValue((rule.Id, f.Key), out var node);

                        // Fill operator cell if present for this field's node
                        if (node?.Operator != null)
                            ws.Cell(row, operatorCol).Value = node.Operator.Value.ToString();

                        string cellValue = string.Empty;

                        if (node?.ValueJson != null)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(node.ValueJson);
                                var element = doc.RootElement;

                                if (element.ValueKind == JsonValueKind.Array)
                                {
                                    var parts = element.EnumerateArray()
                                        .Select(x => x.ToString())
                                        .ToArray();

                                    cellValue = string.Join(",", parts);
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
                            catch (JsonException)
                            {
                                cellValue = node.ValueJson;
                            }
                        }

                        ws.Cell(row, fieldCol).Value = cellValue;
                    }

                    row++;
                }
            }

            // Basic formatting
            ws.Columns().AdjustToContents();

            // Preserve the dropdown columns' width within reasonable limits.
            for (int col = 1; col <= 3 + fields.Count * 2; col++)
            {
                if (ws.Column(col).Width > 40)
                    ws.Column(col).Width = 40;
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
        public async Task<IRemoteStreamContent?> BulkUploadAsync(Guid workflowId, IRemoteStreamContent fileContent)
        {
            if (fileContent == null || fileContent.ContentLength == 0) return null;
            var ruleFields = await _ruleFieldRepository.GetListAsync();
            var fieldsById = ruleFields.ToDictionary(x => x.Id);

            using var buffer = new MemoryStream();
            await using (var upload = fileContent.GetStream())
            {
                await upload.CopyToAsync(buffer);
            }
            buffer.Position = 0;

            using var wb = new XLWorkbook(buffer);
            var ws = wb.Worksheets.First();

            if (ws.FirstRowUsed() == null) return null;

            var headerCells = ws.Row(1).CellsUsed().ToList();
            if (!headerCells.Any()) return null;

            string UnwrapHeader(string h)
            {
                h = h?.Trim() ?? string.Empty;
                var idx = h.IndexOf('(');
                if (idx > 0) return h.Substring(0, idx).Trim();
                return h;
            }

            var colKeys = headerCells.Select(c => UnwrapHeader(c.GetString())).ToList();

            // Build mapping from field key -> operator column index (headers named like 'AgeOperators')
            var operatorSuffix = "Operators";
            var operatorIndexByField = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < colKeys.Count; i++)
            {
                var k = colKeys[i];
                if (k.EndsWith(operatorSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var fieldKey = k.Substring(0, k.Length - operatorSuffix.Length);
                    operatorIndexByField[fieldKey] = i;
                }
            }

            var workflow = await _workflowRepository.GetAsync(workflowId)
                ?? throw new KeyNotFoundException("Workflow not found.");
            var config = workflow.RuleformConfig;
            if (config == null) throw new RuleValidationException("This workflow has no form config assigned.");

            // Built once for the whole sheet: used to validate every row and to build every expression.
            var formConfigDto = BuildFormConfigDto(config, fieldsById);

            var allowed = new Dictionary<string, (RuleFormConfigField Config, RuleField Field)>();
            foreach (var configField in config.RuleFormConfigFields)
            {
                var configuredField = fieldsById[configField.RulefieldId];
                allowed[configuredField.Key] = (configField, configuredField);
            }

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
                var ruleName = ws.Cell(r, nameIndex + 1).GetString();
                if (string.IsNullOrWhiteSpace(ruleName))
                {
                    // Completely empty rows are ignored; a row with data but no name is a mistake.
                    if (ws.Row(r).CellsUsed().Any(c => !string.IsNullOrWhiteSpace(c.GetString())))
                    {
                        var unnamed = new UploadRow(r);
                        unnamed.Errors.Add("Rule name is required.");
                        rows.Add(unnamed);
                    }
                    continue;
                }

                var row = new UploadRow(r);
                rows.Add(row);

                if (idIndex >= 0)
                {
                    var idText = ws.Cell(r, idIndex + 1).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(idText))
                    {
                        if (Guid.TryParse(idText, out var parsedId)) row.ExistingId = parsedId;
                        else row.Errors.Add($"Invalid ID '{idText}' (expected a GUID).");
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

                var root = new RuleNodeDto { Id = Guid.NewGuid(), NodeType = RuleNodeType.Group };

                for (int c = 0; c < colKeys.Count; c++)
                {
                    var key = colKeys[c];
                    // Skip control columns
                    if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "IsActive", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, "ID", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(key, ErrorsHeader, StringComparison.OrdinalIgnoreCase)) continue;

                    var val = ws.Cell(r, c + 1).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(val)) continue;
                    if (!allowed.TryGetValue(key, out var allowedField)) continue;

                    var cfg = allowedField.Config;
                    var field = allowedField.Field;

                    // Determine operator for this field (per-row operator column named '<FieldKey>Operators')
                    var operatorToUse = RuleOperator.Equal;
                    if (operatorIndexByField.TryGetValue(key, out var opIdx))
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
                        Id = Guid.NewGuid(),
                        NodeType = RuleNodeType.Condition,
                        FieldKey = key,
                        Operator = operatorToUse,
                        IsMultiple = cfg.IsMultiple
                    };

                    if (cfg.IsMultiple || val.Contains('|') || (field.DataType == "List" && val.Contains(',')))
                    {
                        var parts = val.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                        node.Value = JsonDocument.Parse(JsonSerializer.Serialize(parts)).RootElement;
                    }
                    else
                    {
                        node.Value = JsonDocument.Parse(JsonSerializer.Serialize(val)).RootElement;
                    }

                    if (root.Children.Count > 0) node.LogicalJoinOperator = LogicalOperator.And;
                    root.Children.Add(node);
                }

                if (row.Errors.Count > 0) continue;   // cell-level problems found: no point validating the whole rule

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
            if (rows.Any(x => x.Errors.Count > 0))
                return BuildErrorWorkbook(wb, ws, lastRow, rows, workflow.Name);

            // ---- Phase 2: everything is valid; save. All rows are written in one flush (one transaction). ----
            foreach (var row in rows)
            {
                if (row.ExistingId.HasValue)
                {
                    await UpdarteRuleAsync(row.Dto!, row.ExistingId.Value, row.Prepared, saveChanges: false);

                }
                else
                {
                    await SaveRuleAsync(row.Dto!, row.Prepared, saveChanges: false);

                }
            }

            if (rows.Count > 0 && rows.Any(x => x.Errors.Count() > 0))
                await CurrentUnitOfWork.SaveChangesAsync();

            return null;
        }



        private const string ErrorsHeader = "Errors";
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        /// <summary>One data row of the uploaded sheet, with everything that is wrong with it.</summary>
        private sealed class UploadRow
        {
            public UploadRow(int rowNumber) => RowNumber = rowNumber;

            public int RowNumber { get; }
            public Guid? ExistingId { get; set; }
            public CreateRuleDto? Dto { get; set; }
            public PreparedRule? Prepared { get; set; }
            public List<string> Errors { get; } = new();
        }


        /// <summary>Writes each row's problems (comma separated) into an "Errors" column after the last used column.</summary>
        private static IRemoteStreamContent BuildErrorWorkbook(
            XLWorkbook wb, IXLWorksheet ws, int lastRow, List<UploadRow> rows, string? originalFileName)
        {
            var lastColumn = ws.LastColumnUsed().ColumnNumber();
            var lastHeader = ws.Cell(1, lastColumn);

            // Re-uploading a sheet that already has an Errors column: reuse (and refresh) it instead of adding another.
            var errorColumn = string.Equals(lastHeader.GetString().Trim(), ErrorsHeader, StringComparison.OrdinalIgnoreCase)
                ? lastColumn
                : lastColumn + 1;

            if (errorColumn == lastColumn)
            {
                for (var r = 2; r <= lastRow; r++)
                    ws.Cell(r, errorColumn).Clear(XLClearOptions.Contents);
            }
            else
            {
                ws.Cell(1, errorColumn).Style = lastHeader.Style;   // same look as the other headers
            }

            ws.Cell(1, errorColumn).Value = ErrorsHeader;
            ws.Column(errorColumn).Width = 60;

            foreach (var row in rows.Where(x => x.Errors.Count > 0))
            {
                var cell = ws.Cell(row.RowNumber, errorColumn);
                cell.Value = string.Join(", ", row.Errors);
                cell.Style.Font.FontColor = XLColor.Red;
                cell.Style.Alignment.WrapText = true;
            }

            var output = new MemoryStream();
            wb.SaveAs(output);
            output.Position = 0;

            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Rules_Import";

            return new RemoteStreamContent(output, $"{baseName}_Errors.xlsx", ExcelContentType);
        }
    }
}
