using Volo.Abp.Modularity;

namespace RulesEngine;

[DependsOn(
    typeof(RulesEngineApplicationModule),
    typeof(RulesEngineDomainTestModule)
)]
public class RulesEngineApplicationTestModule : AbpModule
{

}
