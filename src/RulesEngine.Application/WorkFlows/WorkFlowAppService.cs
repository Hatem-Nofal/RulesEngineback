using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using RulesEngine.Permissions;

namespace RulesEngine.WorkFlows
{
    [Authorize(RulesEnginePermissions.Workflows)]
    public class WorkFlowAppService : CrudAppService<WorkFlow, WorkflowDto, WorkflowDto, Guid, WorkflowPagedAndFilterDto, SaveWorkflowDto, SaveWorkflowDto>, IWorkFlowAppService
    {
        private readonly IWorkFlowRepository _workFlowRepository;
        private readonly IRepository<RuleFormConfig, Guid> _ruleFormConfigRepository;
        private readonly IRepository<Rule, Guid> _ruleRepository;

        public WorkFlowAppService(IRepository<WorkFlow, Guid> repository, IWorkFlowRepository workFlowRepository, IRepository<RuleFormConfig, Guid> ruleFormConfigRepository, IRepository<Rule, Guid> ruleRepository) : base(repository)
        {
            _workFlowRepository = workFlowRepository;
            _ruleFormConfigRepository = ruleFormConfigRepository;
            _ruleRepository = ruleRepository;
        }

        public override async Task<PagedResultDto<WorkflowDto>> GetListAsync(WorkflowPagedAndFilterDto input)
        {
            input.Sorting = input.Sorting ?? "CreationTime desc";

            var query = (await _workFlowRepository.GetAllList())
                .WhereIf(input.RejectionLevel.HasValue, x => x.RejectionLevel == input.RejectionLevel)
                .WhereIf(input.RuleFormConfigId.HasValue, x => x.RuleformConfigId == input.RuleFormConfigId)
                .WhereIf(!string.IsNullOrEmpty(input.Name), x => x.Name.Contains(input.Name));
            var total = await AsyncExecuter.CountAsync(query);

            var workflows = query.ToList();


            return new PagedResultDto<WorkflowDto>(total, workflows.Select(w => new WorkflowDto()
            {
                Id = w.Id,
                Name = w.Name,
                RuleFormConfigId = w.RuleformConfigId,
                RuleFormConfigName = w.RuleformConfig.Name,
                RuleCount = w.Rules.Count(),
                HasCustomeRule = w.HasCustomeRule,
                RejectionLevel = w.RejectionLevel,
                RuleExpression = w.HasCustomeRule ? w.Rules.FirstOrDefault().Expression : "",
                ViolationMessage = w.ViolationMessage
            }).ToList());

        }


        public override async Task<WorkflowDto> GetAsync(Guid id)
        {
            var workflow = await _workFlowRepository.GetDetailsAsync(id);
            var configs = (await _ruleFormConfigRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);
            var count = await _ruleRepository.CountAsync(x => x.WorkflowId == id);
            return Map(workflow, configs, new Dictionary<Guid, int> { [id] = count });

        }

        public override async Task<WorkflowDto> CreateAsync(SaveWorkflowDto input)
        {
            var workflow = new WorkFlow(input.Name, input.RuleFormConfigId, input.RejectionLevel, input.HasCustomeRule, input.ViolationMessage);
            workflow.SetId(null);


            if (input.HasCustomeRule)
            {
                if (string.IsNullOrEmpty(input.RuleExpression))
                    throw new RuleValidationException($"If WorkFlow HasCustomeRule , RuleExpression is require !");

                workflow.AddRule(Rule.CreateRuleForCustomeWorkflow(Guid.CreateVersion7(), "Rule-" + workflow.Name + "-1", workflow.Id, input.RuleExpression));
            }
            await _workFlowRepository.InsertAsync(workflow);

            return Map(workflow, new Dictionary<Guid, string>(), new Dictionary<Guid, int> { [workflow.Id] = 0 });
        }

        public override async Task<WorkflowDto> UpdateAsync(Guid id, SaveWorkflowDto input)
        {
            var workflow = await _workFlowRepository.GetAsync(x => x.Id == id);
            workflow.UpdateName(input.Name);
            workflow.UpdateRuleFormConfigId(input.RuleFormConfigId);
            workflow.SetHasCustomeRule(input.HasCustomeRule);
            workflow.SetViolationMessage(input.ViolationMessage);
            workflow.UpdateRuleFormConfigId(input.RuleFormConfigId);
            if (input.HasCustomeRule)
            {
                if (string.IsNullOrEmpty(input.RuleExpression))
                    throw new RuleValidationException($"If WorkFlow HasCustomeRule , RuleExpression is require !");
                if (workflow.Rules.Any())
                {
                    workflow.Rules.FirstOrDefault().UpdateExpression(input.RuleExpression);
                }
                else
                {
                    workflow.AddRule(Rule.CreateRuleForCustomeWorkflow(Guid.CreateVersion7(), "Rule-" + workflow.Name + "-1", workflow.Id, input.RuleExpression));

                }
            }


            await _workFlowRepository.UpdateAsync(workflow);

            return Map(workflow, new Dictionary<Guid, string>(), new Dictionary<Guid, int> { [workflow.Id] = 0 });
        }

        public override async Task DeleteAsync(Guid id)
        {
            var count = await _ruleRepository.CountAsync(x => x.WorkflowId == id);
            if (count > 0)
                throw new RuleValidationException($"Cannot delete: {count} rule(s) use this workflow. Delete or move them first.");

            await _workFlowRepository.DeleteAsync(id);
        }

        private static WorkflowDto Map(WorkFlow w, Dictionary<Guid, string> configs, Dictionary<Guid, int> counts) => new()
        {
            Id = w.Id,
            Name = w.Name,
            RuleFormConfigId = w.RuleformConfigId,
            RuleFormConfigName = w.RuleformConfigId is { } c ? configs.GetValueOrDefault(c) : null,
            RuleCount = counts.GetValueOrDefault(w.Id),
            HasCustomeRule = w.HasCustomeRule,
            RejectionLevel = w.RejectionLevel,
            RuleExpression = w.HasCustomeRule ? w.Rules.FirstOrDefault()?.Expression : string.Empty,
            ViolationMessage = w.ViolationMessage

        };
        private static WorkflowDto Map(WorkflowDto w, Dictionary<Guid, string> configs, Dictionary<Guid, int> counts) => new()
        {
            Id = w.Id,
            Name = w.Name,
            RuleFormConfigId = w.RuleFormConfigId,
            RuleFormConfigName = w.RuleFormConfigId is { } c ? configs.GetValueOrDefault(c) : null,
            RuleCount = counts.GetValueOrDefault(w.Id),
            HasCustomeRule = w.HasCustomeRule,
            RejectionLevel = w.RejectionLevel,
            RuleExpression = w.RuleExpression,
            ViolationMessage = w.ViolationMessage

        };
        private static WorkflowDto Map(WorkFlow w) => new()
        {
            Id = w.Id,
            Name = w.Name,
            RuleFormConfigId = w.RuleformConfigId,
            RuleFormConfigName = w.RuleformConfig.Name,
            RuleCount = w.Rules.Count(),
            HasCustomeRule = w.HasCustomeRule,
            RejectionLevel = w.RejectionLevel,
            RuleExpression = w.HasCustomeRule ? w.Rules.FirstOrDefault().Expression : "",
            ViolationMessage = w.ViolationMessage

        };

    }
}
