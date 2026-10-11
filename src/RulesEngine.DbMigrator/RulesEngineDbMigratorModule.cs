using RulesEngine.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace RulesEngine.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(RulesEngineEntityFrameworkCoreModule),
    typeof(RulesEngineApplicationContractsModule)
)]
public class RulesEngineDbMigratorModule : AbpModule
{
}
