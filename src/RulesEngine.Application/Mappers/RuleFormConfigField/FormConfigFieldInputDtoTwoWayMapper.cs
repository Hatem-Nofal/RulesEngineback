using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class FormConfigFieldInputDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<FormConfigFieldInputDto, RuleFormConfigField>
    {
        public override partial RuleFormConfigField Map(FormConfigFieldInputDto source);
        public override partial void Map(FormConfigFieldInputDto source, RuleFormConfigField destination);

        public override partial FormConfigFieldInputDto ReverseMap(RuleFormConfigField source);
        public override partial void ReverseMap(RuleFormConfigField source, FormConfigFieldInputDto destination);
    }
}
