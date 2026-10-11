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

namespace RulesEngine.WorkFlows
{
    public class WorkFlowAppService : CrudAppService<WorkFlow, WorkflowDto, WorkflowDto, Guid, WorkflowPagedAndFilterDto, SaveWorkflowDto, SaveWorkflowDto>, IWorkFlowAppService
    {
        private readonly IWorkFlowRepository _workFlowRepository;
        private readonly IRepository<RuleFormConfig> _ruleFormConfigRepository;
        private readonly IRepository<Rule> _ruleRepository;

        public WorkFlowAppService(IRepository<WorkFlow, Guid> repository, IWorkFlowRepository workFlowRepository, IRepository<RuleFormConfig> ruleFormConfigRepository, IRepository<Rule> ruleRepository) : base(repository)
        {
            _workFlowRepository = workFlowRepository;
            _ruleFormConfigRepository = ruleFormConfigRepository;
            _ruleRepository = ruleRepository;
        }

        public override async Task<PagedResultDto<WorkflowDto>> GetListAsync(WorkflowPagedAndFilterDto input)
        {
            input.Sorting = input.Sorting ?? "CreationTime desc";

            var configs = (await _ruleFormConfigRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);
            var counts = (await _ruleRepository.GetListAsync())
                .GroupBy(x => x.WorkflowId)
                .Select(g => new { WorkflowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.WorkflowId, x => x.Count);
            var workflows = await base.GetListAsync(input);
            var workflowDtos = workflows.Items.Select(w => Map(w, configs, counts)).ToList();
            return new PagedResultDto<WorkflowDto>(workflows.TotalCount, workflowDtos);

        }


        public override async Task<WorkflowDto> GetAsync(Guid id)
        {
            var workflow = await _workFlowRepository.GetAsync(id);
            var configs = (await _ruleFormConfigRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);
            var count = await _ruleRepository.CountAsync(x => x.WorkflowId == id);

            return Map(workflow, configs, new Dictionary<Guid, int> { [id] = count });

        }

        public override async Task<WorkflowDto> CreateAsync(SaveWorkflowDto input)
        {
            var workflow = new WorkFlow(GuidGenerator.Create(), input.Name, input.RuleFormConfigId);    

             await _workFlowRepository.InsertAsync(workflow);

            return Map(workflow, new Dictionary<Guid, string>(), new Dictionary<Guid, int> { [workflow.Id] = 0 });
        }

        public override async Task<WorkflowDto> UpdateAsync(Guid id, SaveWorkflowDto input)
        {
            var workflow = await _workFlowRepository.GetAsync(id);
            workflow.UpdateName(input.Name);
            workflow.UpdateRuleFormConfigId(input.RuleFormConfigId) ;

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
            RuleFormConfigId = w.RuleformconfigId,
            RuleFormConfigName = w.RuleformconfigId is { } c ? configs.GetValueOrDefault(c) : null,
            RuleCount = counts.GetValueOrDefault(w.Id)
        };
        private static WorkflowDto Map(WorkflowDto w, Dictionary<Guid, string> configs, Dictionary<Guid, int> counts) => new()
        {
            Id = w.Id,
            Name = w.Name,
            RuleFormConfigId = w.RuleFormConfigId,
            RuleFormConfigName = w.RuleFormConfigId is { } c ? configs.GetValueOrDefault(c) : null,
            RuleCount = counts.GetValueOrDefault(w.Id)
        };

    }
}
