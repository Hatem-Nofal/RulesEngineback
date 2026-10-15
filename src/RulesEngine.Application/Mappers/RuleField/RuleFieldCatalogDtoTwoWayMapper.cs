using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFieldCatalogDtoTwoWayMapper : Volo.Abp.Mapperly.TwoWayMapperBase<RuleFieldCatalogDto, RuleField>
    {
        public override partial RuleField Map(RuleFieldCatalogDto source);
        public override partial void Map(RuleFieldCatalogDto source, RuleField destination);

        public override partial RuleFieldCatalogDto ReverseMap(RuleField source);
        public override partial void ReverseMap(RuleField source, RuleFieldCatalogDto destination);
    }
}
