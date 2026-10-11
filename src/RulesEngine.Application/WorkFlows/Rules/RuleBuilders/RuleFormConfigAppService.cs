using RulesEngine.Application.Repositories.WorkFlows;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Lookups.Dtos;
using RulesEngine.Repositories.WorkFlows;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using RulesEngine.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;


namespace RuleBuilder.Api;

/// <summary>Read-only: configs are managed in the database (see DbSeeder or plain SQL).</summary>
public class RuleFormConfigAppService : CrudAppService<RuleFormConfig, RuleFormConfigDto, FormConfigListDto, Guid, PagedAndSortedResultRequestDto, FormConfigEditDto, FormConfigEditDto>, IRuleFormConfigAppService 
{
    private readonly IFormConfigRepository _formRepo;
    private readonly IRepository<RuleFormConfig, Guid> _ruleFormConfigsRepository;
    private readonly IRepository<RuleField, Guid> _ruleFieldsRepository;
    private readonly IWorkFlowRepository _workFlowRepository;


    public RuleFormConfigAppService(IRepository<RuleFormConfig, Guid> repository, IFormConfigRepository formRepo, IRepository<RuleFormConfig, Guid> ruleFormConfigsRepository, IWorkFlowRepository workFlowRepository, IRepository<RuleField, Guid> ruleFieldsRepository) : base(repository)
    {
        _formRepo = formRepo;
        _ruleFormConfigsRepository = ruleFormConfigsRepository;
        _workFlowRepository = workFlowRepository;
        _ruleFieldsRepository = ruleFieldsRepository;
    }

    public async Task<List<LookupDto>> GetConfigsAsync() =>(await _formRepo.GetConfigsAsync()).Select(x => new LookupDto(x.Id, x.Name)).ToList();


 

    public async Task<RuleFormConfigDto?> GetByWorkflowAsync(Guid workflowId) {
        var workflow = await _workFlowRepository.GetAsync(workflowId);


        var configId = workflow.RuleformconfigId;
        var configs = await _ruleFormConfigsRepository.WithDetailsAsync(x => x.RuleFormConfigFields);
      var config = configs.FirstOrDefault(x => x.Id == configId && x.IsActive);
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
                    Options = f.OptionsJson is null ? null : JsonSerializer.Deserialize<List<string>>(f.OptionsJson),
                    AllowedOperators = x.AllowedOperatorsJson is null
                        ? null
                        : JsonSerializer.Deserialize<List<int>>(x.AllowedOperatorsJson)
                };
            }).ToList()
        };
    }


    public override async Task<PagedResultDto<FormConfigListDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        input.Sorting ??= "CreationTime desc";

        return await base.GetListAsync(input);
    }

    public override async Task<RuleFormConfigDto> CreateAsync(FormConfigEditDto input)
    {
        var newRruleFormConfig = new RuleFormConfig(input.Name?.Trim(), input.Description?.Trim());
        newRruleFormConfig.AddFields(input.Fields.Select((x,i) => new RuleFormConfigField(newRruleFormConfig.Id, x.FieldId, i+1, $"[{string.Join(",", x.AllowedOperators)}]")).ToList());
        var ruleFormConfig = await _formRepo.CreateAsync(newRruleFormConfig);
        return await MapToGetOutputDtoAsync(ruleFormConfig);

    }

    public override async Task<RuleFormConfigDto> UpdateAsync(Guid id, FormConfigEditDto input)
    {
        var newRruleFormConfig = new RuleFormConfig(input.Name?.Trim(), input.Description?.Trim());
        newRruleFormConfig.AddFields(input.Fields.Select((x, i) => new RuleFormConfigField(newRruleFormConfig.Id, x.FieldId, i + 1, $"[{string.Join(",", x.AllowedOperators)}]")).ToList());
        var ruleFormConfig = await _formRepo.UpdateAsync(id, newRruleFormConfig);

        return await MapToGetOutputDtoAsync(ruleFormConfig);

    }
    public override async Task DeleteAsync(Guid id)
    {
        await _formRepo.DeleteAsync(id);
    }

}

