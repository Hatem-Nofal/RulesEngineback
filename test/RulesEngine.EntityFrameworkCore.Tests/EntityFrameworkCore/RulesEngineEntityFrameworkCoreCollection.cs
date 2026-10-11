using Xunit;

namespace RulesEngine.EntityFrameworkCore;

[CollectionDefinition(RulesEngineTestConsts.CollectionDefinitionName)]
public class RulesEngineEntityFrameworkCoreCollection : ICollectionFixture<RulesEngineEntityFrameworkCoreFixture>
{

}
