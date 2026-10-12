using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using RulesEngine.WorkFlows.Rules.Helpers;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace RulesEngine.WorkFlows.Rules
{
    public class RuleAppService : CrudAppService<Rule, RuleDto, RuleListItemDto, Guid, PagedAndSortedResultRequestDto, RuleDto, RuleDto>, IRuleAppService
    {
        private readonly IRepository<RuleNode, Guid> _ruleNodeRepository;
        private readonly IRepository<WorkFlow, Guid> _workflowRepository;
        private readonly IRepository<RuleFormConfig, Guid> _formConfigRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly RuleExpressionBuilder _expressionsBuilder;

        public RuleAppService(
            IRepository<Rule, Guid> repository,
            IRepository<RuleNode, Guid> ruleNodeRepository,
            IRepository<WorkFlow, Guid> workflowRepository,
            IRepository<RuleFormConfig, Guid> formConfigRepository,
            IGuidGenerator guidGenerator,
            RuleExpressionBuilder expressionsBuilder) : base(repository)
        {
            _ruleNodeRepository = ruleNodeRepository;
            _workflowRepository = workflowRepository;
            _formConfigRepository = formConfigRepository;
            _guidGenerator = guidGenerator;
            _expressionsBuilder = expressionsBuilder;
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
            var rule = await Repository.FirstOrDefaultAsync(x => x.Id == id)
                       ?? throw new KeyNotFoundException("Rule not found.");
            var nodes = (await _ruleNodeRepository.GetListAsync(x => x.RuleId == id)).OrderBy(x => x.Order).ToList();

            return new RuleDto
            {
                Id = rule.Id,
                Name = rule.Name,
                WorkflowId = rule.WorkflowId,
                IsActive = rule.IsActive,
                Root = RuleTreeMapper.ToTree(nodes)
            };
        }

        public override async Task<RuleDto> CreateAsync(RuleDto input)
        {
            var id = await SaveRuleAsync(input);
            return await GetAsync(id);
        }

        public override async Task<RuleDto> UpdateAsync(Guid id, RuleDto input)
        {
            input.Id = id;
            var ruleId = await SaveRuleAsync(input);
            return await GetAsync(ruleId);
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

        private async Task<Guid> SaveRuleAsync(RuleDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new RuleValidationException("Rule name is required.");

            var workflow = await _workflowRepository.FirstOrDefaultAsync(w => w.Id == input.WorkflowId)
                           ?? throw new RuleValidationException("Workflow not found.");

            var config = input.WorkflowId == Guid.Empty
                ? null
                : await _formConfigRepository.FirstOrDefaultAsync(f => f.Id == workflow.RuleformconfigId);

            if (config == null)
                throw new RuleValidationException("This workflow has no form config assigned.");
            var configdto= new RuleFormConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Fields = config.RuleFormConfigFields.Select(f => new RuleFormFieldDto
                {
                    Id = f.RulefieldId,
                    Key = f.Rulefield.Key,
                    DataType = f.Rulefield.DataType,
                    AllowedOperators = JsonSerializer.Deserialize<List<int>>(f.AllowedOperatorsJson),
                    Label = f.Rulefield.Label,
                    Options = JsonSerializer.Deserialize<List<string>>(f.Rulefield.OptionsJson)
                }).ToList()
            };
            RuleTreeValidator.Validate(input.Root, configdto);

            var expressionsRes = await _expressionsBuilder.BuildForRuleAsync(input);

            var ruleId = input.Id ?? _guidGenerator.Create();

            var existing = input.Id is null ? null : await Repository.FirstOrDefaultAsync(r => r.Id == input.Id.Value);
            if (existing != null)
            {
                var existingNodes = await _ruleNodeRepository.GetListAsync(n => n.RuleId == existing.Id);
                foreach (var n in existingNodes)
                {
                    await _ruleNodeRepository.DeleteAsync(n);
                }

                await Repository.DeleteAsync(existing.Id);
            }

            var rule = new Rule(ruleId, input.Name.Trim(), input.WorkflowId, expressionsRes.Expression);
            if (input.IsActive) rule.Activate(); else rule.Deactivate();

            await Repository.InsertAsync(rule);

            var flat = new List<RuleNode>();
            RuleTreeMapper.Flatten(input.Root, rule.Id, null, 0, flat);
            foreach (var node in flat)
            {
                await _ruleNodeRepository.InsertAsync(node);
            }

            return rule.Id;
        }

         public Task<RuleExpressionResultDto> PreviewExpression(  RuleDto input) => _expressionsBuilder.BuildForRuleAsync(input);
    }
}
