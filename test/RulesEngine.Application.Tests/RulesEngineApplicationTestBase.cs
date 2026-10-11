using Volo.Abp.Modularity;

namespace RulesEngine;

public abstract class RulesEngineApplicationTestBase<TStartupModule> : RulesEngineTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
