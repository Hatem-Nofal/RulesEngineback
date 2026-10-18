using ClosedXML.Excel;
using RulesEngine.Common.Enums;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.IntegrationsNew.RulesEngineManagers;
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
        private readonly IRepository<RuleFormConfigField, Guid> _ruleFormConfigFieldRepository;
        private readonly IWorkFlowRepository _workflowRepository;
        private readonly IRepository<RuleFormConfig, Guid> _formConfigRepository;
        private readonly IRepository<RuleField, Guid> _ruleFieldRepository;
        private readonly IRuleExpressionBuilder _expressionsBuilder;
        private readonly IRuleRepository _ruleRepository;

        private IRulesEngineManager EngineManager => LazyServiceProvider.LazyGetRequiredService<IRulesEngineManager>();

        public RuleAppService(
            IRepository<Rule, Guid> repository,
            IRepository<RuleNode, Guid> ruleNodeRepository,
           IWorkFlowRepository workflowRepository,
            IRepository<RuleFormConfig, Guid> formConfigRepository,
             IRuleExpressionBuilder expressionsBuilder,
            IRepository<RuleFormConfigField, Guid> ruleFormConfigFieldRepository,
            IRuleRepository ruleRepository,
            IRepository<RuleField, Guid> ruleFieldRepository) : base(repository)
        {
            _ruleNodeRepository = ruleNodeRepository;
            _workflowRepository = workflowRepository;
            _formConfigRepository = formConfigRepository;
            _expressionsBuilder = expressionsBuilder;
            _ruleFormConfigFieldRepository = ruleFormConfigFieldRepository;
            _ruleRepository = ruleRepository;
            _ruleFieldRepository = ruleFieldRepository;
        }

        public override async Task<PagedResultDto<RuleListItemDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            input.Sorting = input.Sorting ?? "CreationTime desc";

            var workflows = (await _workflowRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);
            var allRules = (await Repository.GetListAsync()).OrderBy(x => x.Name).ToList();
            var total = allRules.Count;

            var paged = allRules.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

            var items = paged.Select(x => new RuleListItemDto
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
            var ruleFormConfigFields = rule.Workflow.Ruleformconfig.RuleFormConfigFields.ToList();
            var nodes = rule.Nodes.OrderBy(x => x.Order).ToList();

            return new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                WorkflowId = rule.WorkflowId,
                IsActive = rule.IsActive,
                Root = RuleTreeMapper.ToTree(nodes, ruleFormConfigFields)
            };
        }

        public override async Task<RuleDto> CreateAsync(CreateRuleDto input)
        {
            return await SaveRuleAsync(input, null);
        }

        public override async Task<RuleDto> UpdateAsync(Guid id, CreateRuleDto input)
        {
            return await SaveRuleAsync(input, id);
        }

        public override async Task DeleteAsync(Guid id)
        {
            var rule = await Repository.GetAsync(x => x.Id == id);
            var nodes = await _ruleNodeRepository.GetListAsync(n => n.RuleId == id);
            foreach (var n in nodes)
            {
                await _ruleNodeRepository.HardDeleteAsync(n);
            }

            await Repository.HardDeleteAsync(rule);
            await EngineManager.ReloadAfterCommitAsync();
        }

        /// <summary>Validated rule definition: everything needed to save, computed without any database write.</summary>
        private sealed record PreparedRule(RuleDto Dto, string Expression);

        /// <summary>Validates the rule tree against the workflow's form config and builds the expression. Never writes.</summary>
        private async Task<PreparedRule> PrepareRuleAsync(CreateRuleDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new RuleValidationException("Rule name is required.");

            var workflow = await _workflowRepository.GetAsync(input.WorkflowId)
                           ?? throw new RuleValidationException("Workflow not found.");
            var ruleFields = (await _ruleFieldRepository.GetListAsync());
            var config = workflow.Ruleformconfig;

            if (config == null)
                throw new RuleValidationException("This workflow has no form config assigned.");
            var configdto = new RuleFormConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                AllowNestedGroups = config.AllowNestedGroups,
                StrategyKey = config.StrategyKey,
                Fields = config.RuleFormConfigFields.Select(f => new RuleFormFieldDto
                {
                    Id = f.RulefieldId,
                    Key = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).Key,
                    DataType = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).DataType,
                    AllowedOperators = ListExtensions.DeserializeIntList(f.AllowedOperatorsJson),
                    Label = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).Label,
                    Options = ListExtensions.DeserializeStringList(ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).OptionsJson),
                    IsMultiple = f.IsMultiple
                }).ToList()
            };
            RuleTreeValidator.Validate(input.Root, configdto);
            var ruleDto = ObjectMapper.Map<CreateRuleDto, RuleDto>(input);
            var expressionsRes = await _expressionsBuilder.BuildForRuleAsync(ruleDto);

            return new PreparedRule(ruleDto, expressionsRes.Expression);
        }
        private async Task<RuleFormConfigDto> PrepareRuleFormConfigDtoAsync(CreateRuleDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new RuleValidationException("Rule name is required.");

            var workflow = await _workflowRepository.GetAsync(input.WorkflowId)
                           ?? throw new RuleValidationException("Workflow not found.");
            var ruleFields = (await _ruleFieldRepository.GetListAsync());
            var config = workflow.Ruleformconfig;

            if (config == null)
                throw new RuleValidationException("This workflow has no form config assigned.");
            var configdto = new RuleFormConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                AllowNestedGroups = config.AllowNestedGroups,
                StrategyKey = config.StrategyKey,
                Fields = config.RuleFormConfigFields.Select(f => new RuleFormFieldDto
                {
                    Id = f.RulefieldId,
                    Key = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).Key,
                    DataType = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).DataType,
                    AllowedOperators = ListExtensions.DeserializeIntList(f.AllowedOperatorsJson),
                    Label = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).Label,
                    Options = ListExtensions.DeserializeStringList(ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).OptionsJson),
                    IsMultiple = f.IsMultiple
                }).ToList()
            };
            return configdto;
        }

        private async Task<RuleDto> SaveRuleAsync(CreateRuleDto input, Guid? id, PreparedRule? prepared = null)
        {
            prepared ??= await PrepareRuleAsync(input);
            var ruleDto = prepared.Dto;

            var ruleId = id ?? Guid.CreateVersion7();

            var rule = new Rule(ruleId, input.Name.Trim(), input.WorkflowId, prepared.Expression);
            if (id.HasValue)
            {
                rule.Update(input.Name.Trim(), input.WorkflowId, prepared.Expression);
                var existingNodes = await _ruleNodeRepository.GetListAsync(n => n.RuleId == rule.Id);
                foreach (var n in existingNodes)
                {
                    await _ruleNodeRepository.HardDeleteAsync(n);
                }
                if (existingNodes.Any())
                {
                    rule.ResetNodes();
                }
            }




            if (input.IsActive) rule.Activate(); else rule.Deactivate();


            var flatnodes = new List<RuleNode>();
            RuleTreeMapper.Flatten(input.Root, rule.Id, null, 0, flatnodes);

            rule.AddNodes(flatnodes);
            try
            {
                if (!id.HasValue)
                {
                    await _ruleRepository.InsertAsync(rule);

                }
                else
                {
                    await _ruleRepository.UpdateAsync(rule);

                }



                if (rule.Nodes.Any())
                {
                    await _ruleNodeRepository.InsertManyAsync(rule.Nodes);
                    await CurrentUnitOfWork.SaveChangesAsync();

                }

            }
            catch (Exception ex)
            {

                throw;
            }
            await EngineManager.ReloadAfterCommitAsync();   // once per unit of work, even for bulk uploads
            return ruleDto;
        }

        public Task<RuleExpressionResultDto> PreviewExpression(RuleDto input) => _expressionsBuilder.BuildForRuleAsync(input);


        public async Task<IRemoteStreamContent> GetRulesSampleAsync(Guid workflowId)
        {
            var workflow = await _workflowRepository.GetAsync(workflowId)
                ?? throw new KeyNotFoundException("Workflow not found.");

            var config = workflow.Ruleformconfig;

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

                var allNodes = (await _ruleNodeRepository.GetListAsync(
                    n => ruleIds.Contains(n.RuleId)))
                    .ToList();

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

                        var node = allNodes.FirstOrDefault(
                            n => n.RuleId == rule.Id && n.FieldKey == f.Key);

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

        /// <summary>
        /// Imports rules from an Excel sheet (see <see cref="GetRulesSampleAsync"/>).
        /// Every row is validated first and nothing is saved unless ALL rows are valid.
        /// Returns null on success; when any row has a problem, returns the same workbook with the problems
        /// written to an "Errors" column at the end of each affected row (several problems: comma separated).
        /// </summary>
        public async Task<IRemoteStreamContent?> BulkUploadAsync(Guid workflowId, IRemoteStreamContent fileContent)
        {
            if (fileContent == null || fileContent.ContentLength == 0) return null;
            var ruleFields = (await _ruleFieldRepository.GetListAsync()).ToHashSet();

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
            var config = workflow.Ruleformconfig;
            if (config == null) throw new RuleValidationException("This workflow has no form config assigned.");

            var configFields = config.RuleFormConfigFields.OrderBy(f => f.Order).ToList();

            var configFieldIds = configFields.Select(x => x.RulefieldId).ToHashSet();
            var fields = await _ruleFieldRepository.GetListAsync(x => configFieldIds.Contains(x.Id));

            var allowed = config.RuleFormConfigFields.ToDictionary(cf => fields.FirstOrDefault(x => x.Id == cf.RulefieldId).Key, cf => cf);

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
                    if (!allowed.ContainsKey(key)) continue;

                    var cfg = allowed[key];
                    var field = fields.FirstOrDefault(x => x.Id == cfg.RulefieldId);

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

                    var ruleField = ruleFields.FirstOrDefault(x => x.Key == key);
                    var configField = configFields.FirstOrDefault(x => x.RulefieldId == ruleField.Id);
                    var node = new RuleNodeDto
                    {
                        Id = Guid.NewGuid(),
                        NodeType = RuleNodeType.Condition,
                        FieldKey = key,
                        Operator = operatorToUse,
                        IsMultiple = configField.IsMultiple
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

                row.Dto = new CreateRuleDto { Name = ruleName.Trim(), WorkflowId = workflowId, IsActive = isActive, Root = root };
                try
                {
                    row.Prepared = await PrepareRuleAsync(row.Dto);
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
            
            // ---- Phase 2: everything is valid; save. ----
            foreach (var row in rows)
            {
                var configdto = await PrepareRuleFormConfigDtoAsync(row.Dto!);
                row.Errors.AddRange( RuleTreeValidator.ValidateWitErrors(row.Dto.Root!, configdto));

                if (!row.Errors.Any())
                    await SaveRuleAsync(row.Dto!, row.ExistingId, row.Prepared);

            }
            if (rows.Any(x => x.Errors.Count > 0))
                return BuildErrorWorkbook(wb, ws, lastRow, rows, fileContent.FileName);

            return null;
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




    //public class RuleAppService : CrudAppService<Rule, RuleDto, RuleListItemDto, Guid, PagedAndSortedResultRequestDto, CreateRuleDto, CreateRuleDto>, IRuleAppService
    //{
    //    private readonly IRepository<RuleNode, Guid> _ruleNodeRepository;
    //    private readonly IRepository<RuleFormConfigField, Guid> _ruleFormConfigFieldRepository;
    //    private readonly IWorkFlowRepository _workflowRepository;
    //    private readonly IRepository<RuleFormConfig, Guid> _formConfigRepository;
    //    private readonly IRepository<RuleField, Guid> _ruleFieldRepository;
    //    private readonly IRuleExpressionBuilder _expressionsBuilder;
    //    private readonly IRuleRepository _ruleRepository;

    //    public RuleAppService(
    //        IRepository<Rule, Guid> repository,
    //        IRepository<RuleNode, Guid> ruleNodeRepository,
    //       IWorkFlowRepository workflowRepository,
    //        IRepository<RuleFormConfig, Guid> formConfigRepository,
    //         IRuleExpressionBuilder expressionsBuilder,
    //        IRepository<RuleFormConfigField, Guid> ruleFormConfigFieldRepository,
    //        IRuleRepository ruleRepository,
    //        IRepository<RuleField, Guid> ruleFieldRepository) : base(repository)
    //    {
    //        _ruleNodeRepository = ruleNodeRepository;
    //        _workflowRepository = workflowRepository;
    //        _formConfigRepository = formConfigRepository;
    //        _expressionsBuilder = expressionsBuilder;
    //        _ruleFormConfigFieldRepository = ruleFormConfigFieldRepository;
    //        _ruleRepository = ruleRepository;
    //        _ruleFieldRepository = ruleFieldRepository;
    //    }

    //    public override async Task<PagedResultDto<RuleListItemDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    //    {
    //        input.Sorting = input.Sorting ?? "CreationTime desc";

    //        var workflows = (await _workflowRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);
    //        var allRules = (await Repository.GetListAsync()).OrderBy(x => x.Name).ToList();
    //        var total = allRules.Count;

    //        var paged = allRules.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

    //        var items = paged.Select(x => new RuleListItemDto
    //        {
    //            Id = x.Id,
    //            Name = x.Name,
    //            WorkflowId = x.WorkflowId,
    //            WorkflowName = workflows.GetValueOrDefault(x.WorkflowId),
    //            IsActive = x.IsActive
    //        }).ToList();

    //        return new PagedResultDto<RuleListItemDto>(total, items);
    //    }

    //    public override async Task<RuleDto> GetAsync(Guid id)
    //    {
    //        var rule = await _ruleRepository.GetAsync(id)
    //                   ?? throw new KeyNotFoundException("Rule not found.");
    //        var ruleFormConfigFields = rule.Workflow.Ruleformconfig.RuleFormConfigFields.ToList();
    //        var nodes = rule.Nodes.OrderBy(x => x.Order).ToList();

    //        return new RuleDto
    //        {
    //            Id = rule.Id,
    //            Name = rule.Name,
    //            WorkflowId = rule.WorkflowId,
    //            IsActive = rule.IsActive,
    //            Root = RuleTreeMapper.ToTree(nodes, ruleFormConfigFields)
    //        };
    //    }

    //    public override async Task<RuleDto> CreateAsync(CreateRuleDto input)
    //    {
    //        return await SaveRuleAsync(input, null);
    //    }

    //    public override async Task<RuleDto> UpdateAsync(Guid id, CreateRuleDto input)
    //    {
    //        return await SaveRuleAsync(input, id);
    //    }

    //    public override async Task DeleteAsync(Guid id)
    //    {
    //        var rule = await Repository.GetAsync(x => x.Id == id);
    //        var nodes = await _ruleNodeRepository.GetListAsync(n => n.RuleId == id);
    //        foreach (var n in nodes)
    //        {
    //            await _ruleNodeRepository.HardDeleteAsync(n);
    //        }

    //        await Repository.HardDeleteAsync(rule);
    //    }

    //    private async Task<RuleDto> SaveRuleAsync(CreateRuleDto input, Guid? id)
    //    {
    //        if (string.IsNullOrWhiteSpace(input.Name))
    //            throw new RuleValidationException("Rule name is required.");

    //        var workflow = await _workflowRepository.GetAsync(input.WorkflowId)
    //                       ?? throw new RuleValidationException("Workflow not found.");
    //        var ruleFields = (await _ruleFieldRepository.GetListAsync());
    //        var config = workflow.Ruleformconfig;

    //        if (config == null)
    //            throw new RuleValidationException("This workflow has no form config assigned.");
    //        var configdto = new RuleFormConfigDto
    //        {
    //            Id = config.Id,
    //            Name = config.Name,
    //            AllowNestedGroups = config.AllowNestedGroups,
    //            StrategyKey = config.StrategyKey,
    //            Fields = config.RuleFormConfigFields.Select(f => new RuleFormFieldDto
    //            {
    //                Id = f.RulefieldId,
    //                Key = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).Key,
    //                DataType = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).DataType,
    //                AllowedOperators = ListExtensions.DeserializeIntList(f.AllowedOperatorsJson),
    //                Label = ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).Label,
    //                Options = ListExtensions.DeserializeStringList(ruleFields.FirstOrDefault(x => x.Id == f.RulefieldId).OptionsJson),
    //                IsMultiple = f.IsMultiple
    //            }).ToList()
    //        };
    //        RuleTreeValidator.Validate(input.Root, configdto);
    //        var ruleDto = ObjectMapper.Map<CreateRuleDto, RuleDto>(input);
    //        var expressionsRes = await _expressionsBuilder.BuildForRuleAsync(ruleDto);

    //        var ruleId = id ?? Guid.CreateVersion7();

    //        var rule = new Rule(ruleId, input.Name.Trim(), input.WorkflowId, expressionsRes.Expression);
    //        if (id.HasValue)
    //        {
    //            rule.Update(input.Name.Trim(), input.WorkflowId, expressionsRes.Expression);
    //            var existingNodes = await _ruleNodeRepository.GetListAsync(n => n.RuleId == rule.Id);
    //            foreach (var n in existingNodes)
    //            {
    //                await _ruleNodeRepository.HardDeleteAsync(n);
    //            }
    //            if (existingNodes.Any())
    //            {
    //                rule.ResetNodes();
    //            }
    //        }




    //        if (input.IsActive) rule.Activate(); else rule.Deactivate();


    //        var flatnodes = new List<RuleNode>();
    //        RuleTreeMapper.Flatten(input.Root, rule.Id, null, 0, flatnodes);

    //        rule.AddNodes(flatnodes);
    //        try
    //        {
    //            if (!id.HasValue)
    //            {
    //                await _ruleRepository.InsertAsync(rule);

    //            }
    //            else
    //            {
    //                await _ruleRepository.UpdateAsync(rule);

    //            }



    //            if (rule.Nodes.Any())
    //            {
    //                await _ruleNodeRepository.InsertManyAsync(rule.Nodes);
    //                await CurrentUnitOfWork.SaveChangesAsync();

    //            }

    //        }
    //        catch (Exception ex)
    //        {

    //            throw;
    //        }
    //        return ruleDto;
    //    }

    //    public Task<RuleExpressionResultDto> PreviewExpression(RuleDto input) => _expressionsBuilder.BuildForRuleAsync(input);


    //    public async Task<IRemoteStreamContent> GetRulesSampleAsync(Guid workflowId)
    //    {
    //        var workflow = await _workflowRepository.GetAsync(workflowId)
    //            ?? throw new KeyNotFoundException("Workflow not found.");

    //        var config = workflow.Ruleformconfig;

    //        if (config == null)
    //            throw new RuleValidationException(
    //                "This workflow has no form config assigned.");

    //        var configFields = config.RuleFormConfigFields
    //            .Where(x => x.IsDefault)
    //            .OrderBy(f => f.Order)
    //            .ToList();

    //        var configFieldIds = configFields
    //            .Select(x => x.RulefieldId)
    //            .ToHashSet();

    //        var fields = (await _ruleFieldRepository.GetListAsync(
    //            x => configFieldIds.Contains(x.Id)))
    //            .OrderBy(f => configFields.FindIndex(
    //                cf => cf.RulefieldId == f.Id))
    //            .ToList();

    //        using var workbook = new XLWorkbook();

    //        var ws = workbook.Worksheets.Add("RulesSample");
    //        var optionsSheet = workbook.Worksheets.Add("Options");

    //        // Headers
    //        ws.Cell(1, 1).Value = "Name";
    //        ws.Cell(1, 2).Value = "IsActive";
    //        ws.Cell(1, 3).Value = "ID";

    //        // For each field we will have two columns: <FieldKey> and <FieldKey>Operators
    //        for (int i = 0; i < fields.Count; i++)
    //        {
    //            ws.Cell(1, 4 + i * 2).Value = fields[i].Key;
    //            ws.Cell(1, 5 + i * 2).Value = fields[i].Key + "Operators";
    //        }

    //        ws.Row(1).Style.Font.SetBold();

    //        // Add dropdowns for fields with OptionsJson.
    //        // Each field gets its own options column and named range.
    //        const int lastRowValidation = 1000;

    //        for (int i = 0; i < fields.Count; i++)
    //        {
    //            var f = fields[i];

    //            if (string.IsNullOrWhiteSpace(f.OptionsJson))
    //                continue;

    //            List<string> options;

    //            try
    //            {
    //                options = JsonSerializer.Deserialize<List<string>>(
    //                    f.OptionsJson) ?? new List<string>();
    //            }
    //            catch (JsonException)
    //            {
    //                continue;
    //            }

    //            if (options.Count == 0)
    //                continue;

    //            // Store this field's options in its own column on the hidden sheet.
    //            int optionsCol = i + 1;

    //            for (int j = 0; j < options.Count; j++)
    //            {
    //                optionsSheet.Cell(j + 1, optionsCol).Value = options[j];
    //            }

    //            string optionsColumnLetter =
    //                XLHelper.GetColumnLetterFromNumber(optionsCol);

    //            string rangeName = $"FieldOptions{i + 1}";

    //            workbook.DefinedNames.Add(
    //                rangeName,
    //                $"'Options'!${optionsColumnLetter}$1:" +
    //                $"${optionsColumnLetter}${options.Count}"
    //            );

    //            // A=Name, B=IsActive, C=ID, D onwards= field and operator pairs.
    //            int excelCol = 4 + i * 2; // field value column

    //            var validation = ws.Range(
    //                2, excelCol,
    //                lastRowValidation, excelCol)
    //                .CreateDataValidation();

    //            validation.List(rangeName);
    //            validation.InCellDropdown = true;
    //            validation.IgnoreBlanks = true;
    //            validation.ShowErrorMessage = true;

    //            // Add validation for the operator column (adjacent column)
    //            int operatorCol = excelCol + 1;
    //            var opValidation = ws.Range(2, operatorCol, lastRowValidation, operatorCol).CreateDataValidation();
    //            opValidation.InCellDropdown = true;
    //            opValidation.IgnoreBlanks = true;
    //            opValidation.ShowErrorMessage = true;
    //        }

    //        // Add operator options in the Options sheet and a named range.
    //        var operatorNames = Enum.GetNames(typeof(RuleOperator)).ToList();
    //        int operatorOptionsCol = fields.Count + 1; // after per-field options columns
    //        for (int j = 0; j < operatorNames.Count; j++)
    //        {
    //            optionsSheet.Cell(j + 1, operatorOptionsCol).Value = operatorNames[j];
    //        }

    //        string operatorOptionsColumnLetter = XLHelper.GetColumnLetterFromNumber(operatorOptionsCol);
    //        workbook.DefinedNames.Add(
    //            "OperatorOptions",
    //            $"'Options'!${operatorOptionsColumnLetter}$1:${operatorOptionsColumnLetter}${operatorNames.Count}"
    //        );

    //        // Assign the operator named range to each operator column we created earlier.
    //        for (int i = 0; i < fields.Count; i++)
    //        {
    //            int operatorCol = 5 + i * 2; // matching header placement
    //            var opValidation = ws.Range(2, operatorCol, lastRowValidation, operatorCol).CreateDataValidation();
    //            opValidation.List("OperatorOptions");
    //            opValidation.InCellDropdown = true;
    //            opValidation.IgnoreBlanks = true;
    //            opValidation.ShowErrorMessage = true;
    //        }

    //        // Hide the sheet containing dropdown options.
    //        optionsSheet.Hide();

    //        // Populate sample row or existing rules.
    //        var rules = (await Repository.GetListAsync(
    //            r => r.WorkflowId == workflowId))
    //            .ToList();

    //        int row = 2;

    //        if (!rules.Any())
    //        {
    //            ws.Cell(row, 1).Value = "Sample rule";
    //            ws.Cell(row, 2).Value = true;

    //            for (int i = 0; i < fields.Count; i++)
    //            {
    //                var f = fields[i];
    //                int fieldCol = 4 + i * 2;
    //                int operatorCol = fieldCol + 1;

    //                // default operator = Equal, default value = first option (if available)
    //                ws.Cell(row, operatorCol).Value = RuleOperator.Equal.ToString();

    //                string value = string.Empty;

    //                if (!string.IsNullOrWhiteSpace(f.OptionsJson))
    //                {
    //                    try
    //                    {
    //                        var opts = JsonSerializer.Deserialize<List<string>>(
    //                            f.OptionsJson);

    //                        if (opts?.Count > 0)
    //                            value = opts[0];
    //                    }
    //                    catch (JsonException)
    //                    {
    //                        // Leave the sample value empty if JSON is invalid.
    //                    }
    //                }

    //                ws.Cell(row, fieldCol).Value = value;
    //            }
    //        }
    //        else
    //        {
    //            var ruleIds = rules.Select(r => r.Id).ToList();

    //            var allNodes = (await _ruleNodeRepository.GetListAsync(
    //                n => ruleIds.Contains(n.RuleId)))
    //                .ToList();

    //            foreach (var rule in rules)
    //            {
    //                ws.Cell(row, 1).Value = rule.Name;
    //                ws.Cell(row, 2).Value = rule.IsActive;
    //                ws.Cell(row, 3).Value = rule.Id.ToString();

    //                for (int i = 0; i < fields.Count; i++)
    //                {
    //                    var f = fields[i];
    //                    int fieldCol = 4 + i * 2;
    //                    int operatorCol = fieldCol + 1;

    //                    var node = allNodes.FirstOrDefault(
    //                        n => n.RuleId == rule.Id && n.FieldKey == f.Key);

    //                    // Fill operator cell if present for this field's node
    //                    if (node?.Operator != null)
    //                        ws.Cell(row, operatorCol).Value = node.Operator.Value.ToString();

    //                    string cellValue = string.Empty;

    //                    if (node?.ValueJson != null)
    //                    {
    //                        try
    //                        {
    //                            using var doc = JsonDocument.Parse(node.ValueJson);
    //                            var element = doc.RootElement;

    //                            if (element.ValueKind == JsonValueKind.Array)
    //                            {
    //                                var parts = element.EnumerateArray()
    //                                    .Select(x => x.ToString())
    //                                    .ToArray();

    //                                cellValue = string.Join(",", parts);
    //                            }
    //                            else if (element.ValueKind == JsonValueKind.String)
    //                            {
    //                                cellValue = element.GetString() ?? string.Empty;
    //                            }
    //                            else
    //                            {
    //                                cellValue = element.ToString();
    //                            }
    //                        }
    //                        catch (JsonException)
    //                        {
    //                            cellValue = node.ValueJson;
    //                        }
    //                    }

    //                    ws.Cell(row, fieldCol).Value = cellValue;
    //                }

    //                row++;
    //            }
    //        }

    //        // Basic formatting
    //        ws.Columns().AdjustToContents();

    //        // Preserve the dropdown columns' width within reasonable limits.
    //        for (int col = 1; col <= 3 + fields.Count * 2; col++)
    //        {
    //            if (ws.Column(col).Width > 40)
    //                ws.Column(col).Width = 40;
    //        }

    //        var stream = new MemoryStream();
    //        workbook.SaveAs(stream);
    //        stream.Position = 0;

    //        return new RemoteStreamContent(
    //            stream,
    //            "Rules_Import_Template.xlsx",
    //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    //        );
    //    }




    //    public async Task BulkUploadAsync(Guid workflowId, IRemoteStreamContent fileContent)
    //    {
    //        if (fileContent == null || fileContent.ContentLength == 0) return;
    //        var ruleFields = (await _ruleFieldRepository.GetListAsync()).ToHashSet();

    //        using (var stream = fileContent.GetStream())
    //        {
    //            // Convert the stream to a byte array and save it to the provider (keep for compatibility)
    //            var memoryStream = new MemoryStream();
    //            await stream.CopyToAsync(memoryStream);
    //            using var wb = new XLWorkbook(stream);
    //            var ws = wb.Worksheets.First();

    //            var firstRow = ws.FirstRowUsed();
    //            if (firstRow == null) return;

    //            var headerCells = ws.Row(1).CellsUsed().ToList();
    //            if (!headerCells.Any()) return;

    //            string UnwrapHeader(string h)
    //            {
    //                h = h?.Trim() ?? string.Empty;
    //                var idx = h.IndexOf('(');
    //                if (idx > 0) return h.Substring(0, idx).Trim();
    //                return h;
    //            }

    //            var colKeys = headerCells.Select(c => UnwrapHeader(c.GetString())).ToList();

    //            // Build mapping from field key -> operator column index (headers named like 'AgeOperators')
    //            var operatorSuffix = "Operators";
    //            var operatorIndexByField = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    //            for (int i = 0; i < colKeys.Count; i++)
    //            {
    //                var k = colKeys[i];
    //                if (k.EndsWith(operatorSuffix, StringComparison.OrdinalIgnoreCase))
    //                {
    //                    var fieldKey = k.Substring(0, k.Length - operatorSuffix.Length);
    //                    operatorIndexByField[fieldKey] = i;
    //                }
    //            }

    //            var workflow = await _workflowRepository.GetAsync(workflowId)
    //                ?? throw new KeyNotFoundException("Workflow not found.");
    //            var config = workflow.Ruleformconfig;
    //            if (config == null) throw new RuleValidationException("This workflow has no form config assigned.");

    //            var configFields = config.RuleFormConfigFields.OrderBy(f => f.Order).ToList();

    //            var configFieldIds = configFields.Select(x => x.RulefieldId).ToHashSet();
    //            var fields = await _ruleFieldRepository.GetListAsync(x => configFieldIds.Contains(x.Id));

    //            var allowed = config.RuleFormConfigFields.ToDictionary(cf => fields.FirstOrDefault(x => x.Id == cf.RulefieldId).Key, cf => cf);

    //            var nameIndex = colKeys.FindIndex(c => string.Equals(c, "Name", StringComparison.OrdinalIgnoreCase));
    //            var isActiveIndex = colKeys.FindIndex(c => string.Equals(c, "IsActive", StringComparison.OrdinalIgnoreCase));
    //            var idIndex = colKeys.FindIndex(c => string.Equals(c, "ID", StringComparison.OrdinalIgnoreCase));

    //            var lastRow = ws.LastRowUsed().RowNumber();

    //            for (int r = 2; r <= lastRow; r++)
    //            {
    //                var ruleName = nameIndex >= 0 ? ws.Cell(r, nameIndex + 1).GetString() : null;
    //                if (string.IsNullOrWhiteSpace(ruleName)) continue;

    //                var existingId = (Guid?)null;
    //                if (idIndex >= 0)
    //                {
    //                    var idCell = ws.Cell(r, idIndex + 1).GetString()?.Trim();
    //                    if (!string.IsNullOrWhiteSpace(idCell) && Guid.TryParse(idCell, out var parsedId))
    //                    {
    //                        // Only consider it an update if the rule belongs to the same workflow
    //                        existingId = parsedId;
    //                    }
    //                }

    //                var isActive = true;
    //                if (isActiveIndex >= 0)
    //                {
    //                    var cell = ws.Cell(r, isActiveIndex + 1);
    //                    var s = cell.GetString();
    //                    if (!string.IsNullOrWhiteSpace(s)) bool.TryParse(s, out isActive);
    //                }

    //                var root = new RuleNodeDto { Id = Guid.NewGuid(), NodeType = RuleNodeType.Group };

    //                for (int c = 0; c < colKeys.Count; c++)
    //                {
    //                    var key = colKeys[c];
    //                    // Skip control columns
    //                    if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase)
    //                        || string.Equals(key, "IsActive", StringComparison.OrdinalIgnoreCase)
    //                        || string.Equals(key, "ID", StringComparison.OrdinalIgnoreCase)) continue;

    //                    var cell = ws.Cell(r, c + 1);
    //                    var val = cell.GetString()?.Trim();
    //                    if (string.IsNullOrWhiteSpace(val)) continue;
    //                    if (!allowed.ContainsKey(key)) continue;

    //                    var cfg = allowed[key];
    //                    var field = fields.FirstOrDefault(x => x.Id == cfg.RulefieldId);

    //                    // Determine operator for this field (per-row operator column named '<FieldKey>Operators')
    //                    var operatorToUse = RuleOperator.Equal;
    //                    if (operatorIndexByField.TryGetValue(key, out var opIdx))
    //                    {
    //                        var opCellStr = ws.Cell(r, opIdx + 1).GetString()?.Trim();
    //                        if (!string.IsNullOrWhiteSpace(opCellStr) && Enum.TryParse<RuleOperator>(opCellStr, true, out var parsedOp))
    //                            operatorToUse = parsedOp;
    //                    }
    //                    var ruleField = ruleFields.FirstOrDefault(x => x.Key == key);
    //                    var configField = configFields.FirstOrDefault(x => x.RulefieldId == ruleField.Id);
    //                    var node = new RuleNodeDto
    //                    {
    //                        Id = Guid.NewGuid(),
    //                        NodeType = RuleNodeType.Condition,
    //                        FieldKey = key,
    //                        Operator = operatorToUse,
    //                        IsMultiple = configField.IsMultiple

    //                    };

    //                    if (cfg.IsMultiple || val.Contains('|') || (field.DataType == "List" && val.Contains(',')))
    //                    {
    //                        var parts = val.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
    //                        var json = JsonSerializer.Serialize(parts);
    //                        node.Value = JsonDocument.Parse(json).RootElement;
    //                    }
    //                    else
    //                    {
    //                        var json = JsonSerializer.Serialize(val);
    //                        node.Value = JsonDocument.Parse(json).RootElement;
    //                    }

    //                    if (root.Children.Count > 0) node.LogicalJoinOperator = LogicalOperator.And;
    //                    root.Children.Add(node);
    //                }

    //                if (root.Children.Count == 0)
    //                {
    //                    var emptyRoot = new RuleNodeDto { Id = Guid.NewGuid(), NodeType = RuleNodeType.Group };
    //                    await SaveRuleAsync(new CreateRuleDto { Name = ruleName.Trim(), WorkflowId = workflowId, IsActive = isActive, Root = emptyRoot }, existingId);
    //                }
    //                else
    //                {
    //                    var createDto = new CreateRuleDto { Name = ruleName.Trim(), WorkflowId = workflowId, IsActive = isActive, Root = root };
    //                    // If existingId has a value, update that rule; otherwise create new
    //                    await SaveRuleAsync(createDto, existingId);
    //                }
    //            }
    //        }
    //    }
    //}
}
