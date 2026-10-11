using Volo.Abp.Modularity;

namespace RulesEngine;

/* Inherit from this class for your domain layer tests. */
public abstract class RulesEngineDomainTestBase<TStartupModule> : RulesEngineTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
