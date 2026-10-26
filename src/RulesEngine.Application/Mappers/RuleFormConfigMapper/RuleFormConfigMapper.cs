using DocumentFormat.OpenXml.Wordprocessing;
using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace RulesEngine
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class RuleFormConfigMapper : Volo.Abp.Mapperly.MapperBase<RuleFormConfig, FormConfigListDto>
    {
        public override void Map(RuleFormConfig source, FormConfigListDto destination)
        {
            destination.AllowNestedGroups = source.AllowNestedGroups;
            destination.Description = source.Description;
            destination.FieldCount = source._RuleFormConfigFields.Count;
            destination.WorkflowCount = source._WorkFlows.Count;
            destination.Name = source.Name;
            destination.StrategyKey = source.StrategyKey;
            destination.IsActive = source.IsActive;
            destination.Id = source.Id;


        }

        public override FormConfigListDto Map(RuleFormConfig source)

        {
            return new FormConfigListDto()
            {
                AllowNestedGroups = source.AllowNestedGroups,
                Description = source.Description,
                FieldCount = source._RuleFormConfigFields.Count,
                WorkflowCount = source._WorkFlows.Count,
                Name = source.Name,
                StrategyKey = source.StrategyKey,
                IsActive = source.IsActive,
                Id = source.Id,
            };
           


        }

    }

}



