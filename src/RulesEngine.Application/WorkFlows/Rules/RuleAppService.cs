using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.Helpers;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

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
            var nodes = await _ruleNodeRepository.GetListAsync(n => n.RuleId == id);
            foreach (var n in nodes)
            {
                await _ruleNodeRepository.DeleteAsync(n);
            }

            await Repository.DeleteAsync(id);
        }

        private async Task<RuleDto> SaveRuleAsync(CreateRuleDto input, Guid? id)
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

            var ruleId = id ?? Guid.CreateVersion7();

            var rule = new Rule(ruleId, input.Name.Trim(), input.WorkflowId, expressionsRes.Expression);
            if (id.HasValue && workflow.Rules.Any(x => x.Id == id))
            {
                rule = workflow.Rules.FirstOrDefault(x => x.Id == id);
                rule.Update(input.Name.Trim(), input.WorkflowId, expressionsRes.Expression);
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

            for (int i = 0; i < fields.Count; i++)
            {
                ws.Cell(1, 4 + i).Value = fields[i].Key;
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

                // Store this field's options in its own column.
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

                // A=Name, B=IsActive, C=ID, D onwards=rule fields.
                int excelCol = 4 + i;

                var validation = ws.Range(
                    2, excelCol,
                    lastRowValidation, excelCol)
                    .CreateDataValidation();

                validation.List(rangeName);
                validation.InCellDropdown = true;
                validation.IgnoreBlanks = true;
                validation.ShowErrorMessage = true;
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
                    int col = 4 + i;

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

                    ws.Cell(row, col).Value = value;
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
                        int col = 4 + i;

                        var node = allNodes.FirstOrDefault(
                            n => n.RuleId == rule.Id && n.FieldKey == f.Key);

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

                        ws.Cell(row, col).Value = cellValue;
                    }

                    row++;
                }
            }

            // Basic formatting
            ws.Columns().AdjustToContents();

            // Preserve the dropdown columns' width within reasonable limits.
            for (int col = 1; col <= 3 + fields.Count; col++)
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


        

        public async Task BulkUploadAsync(Guid workflowId, IRemoteStreamContent fileContent)
        {
            if (fileContent == null || fileContent.ContentLength == 0) return;
            using (var stream = fileContent.GetStream())
            {
                // Convert the stream to a byte array and save it to the provider (keep for compatibility)
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.First();

                var firstRow = ws.FirstRowUsed();
                if (firstRow == null) return;

                var headerCells = ws.Row(1).CellsUsed().ToList();
                if (!headerCells.Any()) return;

                string UnwrapHeader(string h)
                {
                    h = h?.Trim() ?? string.Empty;
                    var idx = h.IndexOf('(');
                    if (idx > 0) return h.Substring(0, idx).Trim();
                    return h;
                }

                var colKeys = headerCells.Select(c => UnwrapHeader(c.GetString())).ToList();

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

                var lastRow = ws.LastRowUsed().RowNumber();

                for (int r = 2; r <= lastRow; r++)
                {
                    var ruleName = nameIndex >= 0 ? ws.Cell(r, nameIndex + 1).GetString() : null;
                    if (string.IsNullOrWhiteSpace(ruleName)) continue;

                    var existingId = (Guid?)null;
                    if (idIndex >= 0)
                    {
                        var idCell = ws.Cell(r, idIndex + 1).GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(idCell) && Guid.TryParse(idCell, out var parsedId))
                        {
                            // Only consider it an update if the rule belongs to the same workflow
                            if (workflow.Rules.Any(x => x.Id == parsedId)) existingId = parsedId;
                        }
                    }

                    var isActive = true;
                    if (isActiveIndex >= 0)
                    {
                        var cell = ws.Cell(r, isActiveIndex + 1);
                        var s = cell.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) bool.TryParse(s, out isActive);
                    }

                    var root = new RuleNodeDto { Id = Guid.NewGuid(), NodeType = RulesEngine.Common.Enums.RuleNodeType.Group };

                    for (int c = 0; c < colKeys.Count; c++)
                    {
                        var key = colKeys[c];
                        // Skip control columns
                        if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(key, "IsActive", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(key, "ID", StringComparison.OrdinalIgnoreCase)) continue;

                        var cell = ws.Cell(r, c + 1);
                        var val = cell.GetString()?.Trim();
                        if (string.IsNullOrWhiteSpace(val)) continue;
                        if (!allowed.ContainsKey(key)) continue;

                        var cfg = allowed[key];
                        var field = fields.FirstOrDefault(x => x.Id == cfg.RulefieldId);
                        var node = new RuleNodeDto
                        {
                            Id = Guid.NewGuid(),
                            NodeType = RulesEngine.Common.Enums.RuleNodeType.Condition,
                            FieldKey = key,
                            Operator = RulesEngine.Common.Enums.RuleOperator.Equal
                        };

                        if (cfg.IsMultiple || val.Contains('|') || (field.DataType == "List" && val.Contains(',')))
                        {
                            var parts = val.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                            var json = JsonSerializer.Serialize(parts);
                            node.Value = JsonDocument.Parse(json).RootElement;
                        }
                        else
                        {
                            var json = JsonSerializer.Serialize(val);
                            node.Value = JsonDocument.Parse(json).RootElement;
                        }

                        if (root.Children.Count > 0) node.LogicalJoinOperator = RulesEngine.Common.Enums.LogicalOperator.And;
                        root.Children.Add(node);
                    }

                    if (root.Children.Count == 0)
                    {
                        var emptyRoot = new RuleNodeDto { Id = Guid.NewGuid(), NodeType = RulesEngine.Common.Enums.RuleNodeType.Group };
                        await SaveRuleAsync(new CreateRuleDto { Name = ruleName.Trim(), WorkflowId = workflowId, IsActive = isActive, Root = emptyRoot }, existingId);
                    }
                    else
                    {
                        var createDto = new CreateRuleDto { Name = ruleName.Trim(), WorkflowId = workflowId, IsActive = isActive, Root = root };
                        // If existingId has a value, update that rule; otherwise create new
                        await SaveRuleAsync(createDto, existingId);
                    }
                }
            }
        }
    }
}
