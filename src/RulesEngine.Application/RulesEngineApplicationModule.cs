using EhealthClaims.Lookups;
using Microsoft.Extensions.DependencyInjection;
using RulesEngine.Integrations.RulesEngineManagers;
using RulesEngine.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Rules.RuleExpressionBuilders;
using Volo.Abp.Account;
using Volo.Abp.Application.Services;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace RulesEngine;

[DependsOn(
    typeof(RulesEngineDomainModule),
    typeof(RulesEngineApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class RulesEngineApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            // 2. This line automatically finds and registers 
            // RulesEngineApplicationAutoMapperProfile since it's in this assembly
            options.AddMaps<RulesEngineApplicationModule>();
        });
         context.Services.AddScoped<IRuleExpressionBuilder, RuleExpressionBuilder>();
         context.Services.AddScoped<IRuleExpressionStrategy, RuleExpressionStrategyBase>();
         context.Services.AddScoped<IFieldMapProvider, FieldMapProvider>();
 
         // Register rules engine manager used by IntegrationsAppService
         context.Services.AddScoped<IRulesEngineManager, RulesEngineManager>();
      }

}
