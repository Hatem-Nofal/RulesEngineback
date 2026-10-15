using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.WorkFlows.Dtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class SaveWorkflowDtoToWorkFlowMapper : Volo.Abp.Mapperly.MapperBase<SaveWorkflowDto, WorkFlow>
    {
        public override partial WorkFlow Map(SaveWorkflowDto source);

        public override partial void Map(SaveWorkflowDto source, WorkFlow destination);
    }
}
