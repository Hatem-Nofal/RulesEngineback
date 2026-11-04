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
using System.Linq.Dynamic.Core;

namespace RulesEngine.WorkFlows
{
    [Authorize(RulesEnginePermissions.Workflows)]
    public class WorkFlowAppService : CrudAppService<WorkFlow, WorkflowDto, WorkflowDto, long, WorkflowPagedAndFilterDto, SaveWorkflowDto, SaveWorkflowDto>, IWorkFlowAppService
    {
        private readonly IWorkFlowRepository _workFlowRepository;
        private readonly IRepository<RuleFormConfig, long> _ruleFormConfigRepository;
        private readonly IRepository<Rule, long> _ruleRepository;
        private readonly IRepository<RuleNode, Guid> _ruleNodeRepository;

        public WorkFlowAppService(IRepository<WorkFlow, long> repository, IWorkFlowRepository workFlowRepository, IRepository<RuleFormConfig, long> ruleFormConfigRepository, IRepository<Rule, long> ruleRepository, IRepository<RuleNode, Guid> ruleNodeRepository) : base(repository)
        {
            _workFlowRepository = workFlowRepository;
            _ruleFormConfigRepository = ruleFormConfigRepository;
            _ruleRepository = ruleRepository;
            _ruleNodeRepository = ruleNodeRepository;
        }

        public override async Task<PagedResultDto<WorkflowDto>> GetListAsync(WorkflowPagedAndFilterDto input)
        {
            input.Sorting = input.Sorting ?? "CreationTime desc";

            var query = (await _workFlowRepository.GetAllList())
                .WhereIf(input.RejectionLevel.HasValue, x => x.RejectionLevel == input.RejectionLevel)
                .WhereIf(input.RuleFormConfigId.HasValue, x => x.RuleformConfigId == input.RuleFormConfigId)
                .WhereIf(!string.IsNullOrEmpty(input.Name), x => x.Name.Contains(input.Name));
            var total = await AsyncExecuter.CountAsync(query);
            query = query.OrderBy(input.Sorting ?? "CreationTime DESC")
                .PageBy(input);


            return new PagedResultDto<WorkflowDto>(total, query.ToList().Select(w => new WorkflowDto()
            {
                Id = w.Id,
                Name = w.Name,
                RuleFormConfigId = w.RuleformConfigId,
                RuleFormConfigName = w.RuleformConfig?.Name,
                RuleCount = w.Rules.Count(),
                IsWorkflowLevel = w.IsWorkflowLevel,
                RejectionLevel = w.RejectionLevel,
                RuleExpression = w.IsWorkflowLevel ? w.Rules.FirstOrDefault()?.Expression : "",
                ViolationMessage = w.ViolationMessage
            }).ToList());

        }


        public override async Task<WorkflowDto> GetAsync(long id)
        {
            var workflow = await _workFlowRepository.GetDetailsAsync(id);
            var configs = (await _ruleFormConfigRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);
            var count = await _ruleRepository.CountAsync(x => x.WorkflowId == id);
            return Map(workflow, configs, new Dictionary<long, int> { [id] = count });

        }

        public override async Task<WorkflowDto> CreateAsync(SaveWorkflowDto input)
        {
            var workflow = new WorkFlow(input.Name, input.RuleFormConfigId, input.RejectionLevel, input.IsWorkflowLevel, input.ViolationMessage, input.ViolationType);


            if (input.IsWorkflowLevel)
            {
                if (string.IsNullOrEmpty(input.RuleExpression))
                    throw new RuleValidationException($"If WorkFlow IsWorkflowLevel , RuleExpression is require !");

                workflow.AddRule(Rule.CreateRuleForCustomeWorkflow("Rule-" + workflow.Name + "-1", workflow.Id, input.RuleExpression, input.ViolationMessage, input.ViolationType));
            }
            await _workFlowRepository.InsertAsync(workflow);

            return Map(workflow, new Dictionary<long, string>(), new Dictionary<long, int> { [workflow.Id] = 0 });
        }

        public override async Task<WorkflowDto> UpdateAsync(long id, SaveWorkflowDto input)
        {
            var workflow = await _workFlowRepository.GetForUpdateAsync(id);
            workflow.UpdateName(input.Name);
            workflow.UpdateRuleFormConfigId(input.RuleFormConfigId);
            workflow.SetIsWorkflowLevel(input.IsWorkflowLevel);
            workflow.SetViolationMessage(input.ViolationMessage);
            workflow.UpdateRuleFormConfigId(input.RuleFormConfigId);
            workflow.SetViolationType(input.ViolationType);
            if (input.IsWorkflowLevel)
            {
                if (string.IsNullOrEmpty(input.RuleExpression))
                    throw new RuleValidationException($"If WorkFlow IsWorkflowLevel , RuleExpression is require !");
                if (workflow.Rules.Any())
                {
                    var rule = workflow.Rules.FirstOrDefault();
                    rule.UpdateExpression(input.RuleExpression);
                    rule.UpdateViolationMessage(input.ViolationMessage);
                    rule.UpdateViolationType(input.ViolationType);
                    await _ruleRepository.UpdateAsync(rule);
                }
                else
                {
                    var rule = Rule.CreateRuleForCustomeWorkflow("Rule-" + workflow.Name + "-1", workflow.Id, input.RuleExpression, input.ViolationMessage, input.ViolationType);
                    await _ruleRepository.InsertAsync(rule);
                }
            }


            await _workFlowRepository.UpdateAsync(workflow);
            return Map(workflow, new Dictionary<long, string>(), new Dictionary<long, int> { [workflow.Id] = 0 });
        }

        public override async Task DeleteAsync(long id)
        {

            // One set-based DELETE for all nodes instead of loading them and deleting them one by one.
            await _ruleRepository.DeleteDirectAsync(n => n.WorkflowId == id);
            await _ruleNodeRepository.DeleteDirectAsync(n => n.WorkFlowId == id);

            await _workFlowRepository.HardDeleteAsync(x => x.Id == id);
        }

        private static WorkflowDto Map(WorkFlow w, Dictionary<long, string> configs, Dictionary<long, int> counts) => new()
        {
            Id = w.Id,
            Name = w.Name,
            RuleFormConfigId = w.RuleformConfigId,
            RuleFormConfigName = w.RuleformConfigId is { } c ? configs.GetValueOrDefault(c) : null,
            RuleCount = counts.GetValueOrDefault(w.Id),
            IsWorkflowLevel = w.IsWorkflowLevel,
            RejectionLevel = w.RejectionLevel,
            RuleExpression = w.IsWorkflowLevel ? w.Rules.FirstOrDefault()?.Expression : string.Empty,
            ViolationMessage = w.ViolationMessage,
            ViolationType = w.ViolationType

        };

        public Task<WorkflowForRuleDto> GetWorkflowForRuleAsync(long workflowId)
        {
            var workflow = _workFlowRepository.GetAsync(workflowId).Result;
            if (workflow == null)
                throw new RuleValidationException($"Workflow with Id {workflowId} not found.");
            return Task.FromResult(new WorkflowForRuleDto
            {

                IsWorkflowLevel = workflow.IsWorkflowLevel,
                RuleExpression = workflow.IsWorkflowLevel ? workflow.Rules.FirstOrDefault()?.Expression : string.Empty,
                ViolationMessage = workflow.ViolationMessage,
                ViolationType = workflow.ViolationType
            });
        }
    }
}
