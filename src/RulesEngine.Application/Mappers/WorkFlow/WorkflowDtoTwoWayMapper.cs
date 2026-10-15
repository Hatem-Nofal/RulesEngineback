using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.WorkFlows.Dtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class WorkflowDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<WorkflowDto, WorkFlow>
    {
        public override partial WorkFlow Map(WorkflowDto source);

        public override partial void Map(WorkflowDto source, WorkFlow destination);
        public override partial WorkflowDto ReverseMap(WorkFlow source);

        public override partial void ReverseMap(WorkFlow source, WorkflowDto destination);
    }
}
