using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFormConfigMapper : Volo.Abp.Mapperly.MapperBase<RuleFormConfig, FormConfigListDto>
    {
        public override partial void Map(RuleFormConfig source, FormConfigListDto destination);

        public override partial FormConfigListDto Map(RuleFormConfig source);
    }

}



 