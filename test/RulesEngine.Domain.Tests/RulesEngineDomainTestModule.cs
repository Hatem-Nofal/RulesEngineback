using Volo.Abp.Modularity;

namespace RulesEngine;

[DependsOn(
    typeof(RulesEngineDomainModule),
    typeof(RulesEngineTestBaseModule)
)]
public class RulesEngineDomainTestModule : AbpModule
{

}
