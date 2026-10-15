using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFormConfigFieldToFormConfigFieldInputDtoMapper : Volo.Abp.Mapperly.MapperBase<RuleFormConfigField, FormConfigFieldInputDto>
    {
        public override partial FormConfigFieldInputDto Map(RuleFormConfigField source);
        public override partial void Map(RuleFormConfigField source, FormConfigFieldInputDto destination);
    }
}
