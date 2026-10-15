using AutoMapper;
using Riok.Mapperly.Abstractions;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Dtos.RulesDtos;
using Volo.Abp.AutoMapper;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;

namespace RulesEngine;

/*
 * You can add your own mappings here.
 * [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
 * public partial class RulesEngineApplicationMappers : MapperBase<BookDto, CreateUpdateBookDto>
 * {
 *    public override partial CreateUpdateBookDto Map(BookDto source);
 *
 *    public override partial void Map(BookDto source, CreateUpdateBookDto destination);
 * }
 */
[DependsOn(typeof(AbpAutoMapperModule))]

public class RulesEngineApplicationAutoMapperProfile : Profile
{
    public RulesEngineApplicationAutoMapperProfile()
    {
        CreateMap<WorkFlow, WorkflowDto>();
        CreateMap<WorkflowDto, WorkFlow>();
        CreateMap<SaveWorkflowDto,WorkFlow>();
        CreateMap<Rule, RuleDto>();
        CreateMap<RuleDto, Rule>();
        CreateMap<Rule, RuleListItemDto>();
        CreateMap<RuleListItemDto, Rule>();
        CreateMap<CreateRuleDto, RuleDto>();
        CreateMap<RuleDto,CreateRuleDto>();
        CreateMap<CreateRuleDto, Rule>();
        CreateMap<Rule,CreateRuleDto>();
   
    }
}