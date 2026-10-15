using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.WorkFlows.Dtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class WorkFlowToWorkflowDtoMapper : Volo.Abp.Mapperly.MapperBase<WorkFlow, WorkflowDto>
    {
        public override partial WorkflowDto Map(WorkFlow source);
        public override partial void Map(WorkFlow source, WorkflowDto destination);
    }
}
