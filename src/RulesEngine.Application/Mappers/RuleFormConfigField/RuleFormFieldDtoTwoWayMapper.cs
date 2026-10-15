using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFormFieldDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<RuleFormFieldDto, RuleFormConfigField>
    {
        public override partial RuleFormConfigField Map(RuleFormFieldDto source);
        public override partial void Map(RuleFormFieldDto source, RuleFormConfigField destination);

        public override partial RuleFormFieldDto ReverseMap(RuleFormConfigField source);
        public override partial void ReverseMap(RuleFormConfigField source, RuleFormFieldDto destination);
    }
}
