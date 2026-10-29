using Duende.IdentityModel;
using RulesEngine.Common.Extensions;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Lookups.Dtos;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;


namespace RulesEngine.WorkFlows.Rules.RuleBuilders;

/// <summary>Read-only: configs are managed in the database (see DbSeeder or plain SQL).</summary>
public class RuleFormConfigAppService : CrudAppService<RuleFormConfig, RuleFormConfigDto, FormConfigListDto, Guid, PagedAndSortedResultRequestDto, CreateFormConfigEditDto, CreateFormConfigEditDto>, IRuleFormConfigAppService 
{
     private readonly IRuleFormConfigRepository _ruleFormConfigsRepository;
    private readonly IRepository<RuleFormConfig, Guid> _ruleFormConfig;
    private readonly IRepository<RuleField, Guid> _ruleFieldsRepository;
    private readonly IRepository<RuleFormConfigField, Guid> _ruleFormConfigFieldRepository;
    private readonly IWorkFlowRepository _workFlowRepository;


    public RuleFormConfigAppService(IRepository<RuleFormConfig, Guid> repository, IRuleFormConfigRepository ruleFormConfigsRepository, IWorkFlowRepository workFlowRepository, IRepository<RuleField, Guid> ruleFieldsRepository, IRepository<RuleFormConfigField, Guid> ruleFormConfigFieldRepository, IRepository<RuleFormConfig, Guid> ruleFormConfig) : base(repository)
    {
        _ruleFormConfigsRepository = ruleFormConfigsRepository;
        _workFlowRepository = workFlowRepository;
        _ruleFieldsRepository = ruleFieldsRepository;
        _ruleFormConfigFieldRepository = ruleFormConfigFieldRepository;
        _ruleFormConfig = ruleFormConfig;
    }

    public async Task<List<LookupDto>> GetConfigsAsync() =>(await _ruleFormConfigsRepository.GetConfigsAsync()).Select(x => new LookupDto(x.Id, x.Name)).ToList();

    public override async Task<RuleFormConfigDto> GetAsync(Guid id)
    {
        var ruleFormConfig = await _ruleFormConfigsRepository.GetAsync(id);
         return new RuleFormConfigDto() {
             Id = ruleFormConfig.Id,
             Fields=ruleFormConfig.RuleFormConfigFields.Select(x => new RuleFormFieldDto
             {
                 Id = x.RulefieldId,
                 AllowedOperators = ListExtensions.DeserializeIntList(x.AllowedOperatorsJson),
                 IsMultiple = x.IsMultiple,
                 DataType=x.Rulefield.DataType,
                 Key = x.Rulefield.Key,
                 Label = x.Rulefield.Label,
                 Options = ListExtensions.DeserializeStringList(x.Rulefield.OptionsJson),
                 IsDefault=x.IsDefault,
                 IsDeleted=x.IsDeleted
                  
                 
                 
             }).ToList(),
             AllowNestedGroups = ruleFormConfig.AllowNestedGroups,
             Name = ruleFormConfig.Name,
              StrategyKey = ruleFormConfig.StrategyKey

         } ;
    }

    public async Task<List<RuleFieldCatalogDto>> RuleFields() {
        var ruleFields = await _ruleFieldsRepository.GetQueryableAsync();
        var ruleFieldsList = ruleFields.OrderBy(x => x.Label);
        return ruleFieldsList.Select(f => new RuleFieldCatalogDto
        {
            Id = f.Id,
            Key = f.Key,
            Label = f.Label,
            DataType = f.DataType,
            Options =  ListExtensions.DeserializeStringList(f.OptionsJson),
            
        }).ToList();
    }


    public async Task<RuleFormConfigDto?> GetByWorkflowAsync(Guid workflowId) {
        var workflow = await _workFlowRepository.GetAsync(workflowId);


        var configId = workflow.RuleformConfigId;
        var config = await _ruleFormConfigsRepository.GetAsync(configId.Value);
         if (config is null) return null;

        var ids = config.RuleFormConfigFields.Select(x => x.RulefieldId).ToList();
        var catalog = (await _ruleFieldsRepository.GetListAsync(x => ids.Contains(x.Id))).ToDictionary(x => x.Id);
 
        return new RuleFormConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            AllowNestedGroups = config.AllowNestedGroups,
            StrategyKey = config.StrategyKey,
            Fields = config.RuleFormConfigFields.OrderBy(x => x.Order).Select(x =>
            {
                var f = catalog[x.RulefieldId];
                return new RuleFormFieldDto
                {
                    Id = f.Id,
                    Key = f.Key,
                    Label = f.Label,
                    DataType = f.DataType,
                    Options = ListExtensions.DeserializeStringList(f.OptionsJson),
                    AllowedOperators = ListExtensions.DeserializeIntList(x.AllowedOperatorsJson),
                    IsMultiple=x.IsMultiple
                };
            }).ToList()
        };
    }


    public override async Task<PagedResultDto<FormConfigListDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        input.Sorting ??= "CreationTime desc";

        var query = await _ruleFormConfig.WithDetailsAsync(x => x.WorkFlows, x => x.RuleFormConfigFields);
        var totalCount = await AsyncExecuter.CountAsync(query);

        var entities = new List<RuleFormConfig>();
        var entityDtos = new List<FormConfigListDto>();

        if (totalCount > 0)
        {
            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            entities = await AsyncExecuter.ToListAsync(query);
            entityDtos = await MapToGetListOutputDtosAsync(entities);
        }

        return new PagedResultDto<FormConfigListDto>(
            totalCount,
            entityDtos
        );
    }

    public override async Task<RuleFormConfigDto> CreateAsync(CreateFormConfigEditDto input)
    {
        var newRruleFormConfig = new RuleFormConfig(input.Name?.Trim(), input.AllowNestedGroups, input.Description?.Trim());
        newRruleFormConfig.AddFields(input.Fields.Select((x,i) => new RuleFormConfigField(newRruleFormConfig.Id, x.FieldId, i+1, x.IsMultiple, x.IsDefault, $"[{string.Join(",", x.AllowedOperators)}]")).ToList());
        var ruleFormConfig = await _ruleFormConfigsRepository.CreateAsync(newRruleFormConfig);
        return await MapToGetOutputDtoAsync(ruleFormConfig);

    }

    public override async Task<RuleFormConfigDto> UpdateAsync(Guid id, CreateFormConfigEditDto input)
    {
        var rruleFormConfig =await _ruleFormConfigsRepository.GetAsync(id);
        rruleFormConfig.Update(input.Name?.Trim(), input.Description?.Trim(), input.AllowNestedGroups);
        await _ruleFormConfigFieldRepository.DeleteDirectAsync(n => n.RuleformConfigId == id);
        rruleFormConfig.ResetRuleFormConfigFields();
        rruleFormConfig.AddFields(input.Fields.Where(x=>x.IsDeleted==false).Select((x, i) => new RuleFormConfigField(rruleFormConfig.Id, x.FieldId, i + 1, x.IsMultiple,x.IsDefault, $"[{string.Join(",", x.AllowedOperators)}]")).ToList());
        var ruleFormConfig = await _ruleFormConfigsRepository.UpdateAsync(id, rruleFormConfig);

        return await MapToGetOutputDtoAsync(ruleFormConfig);

    }
    public override async Task DeleteAsync(Guid id)
    {
        await _ruleFormConfigsRepository.DeleteAsync(id);
    }

}

