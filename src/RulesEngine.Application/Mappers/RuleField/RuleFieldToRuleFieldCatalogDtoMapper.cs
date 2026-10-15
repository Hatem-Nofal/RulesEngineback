using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFieldToRuleFieldCatalogDtoMapper : Volo.Abp.Mapperly.MapperBase<RuleField, RuleFieldCatalogDto>
    {
        public override partial RuleFieldCatalogDto Map(RuleField source);
        public override partial void Map(RuleField source, RuleFieldCatalogDto destination);
    }
}
