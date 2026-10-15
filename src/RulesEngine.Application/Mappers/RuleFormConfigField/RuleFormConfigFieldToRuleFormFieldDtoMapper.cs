using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFormConfigFieldToRuleFormFieldDtoMapper : Volo.Abp.Mapperly.MapperBase<RuleFormConfigField, RuleFormFieldDto>
    {
        public override partial RuleFormFieldDto Map(RuleFormConfigField source);
        public override partial void Map(RuleFormConfigField source, RuleFormFieldDto destination);
    }
}
