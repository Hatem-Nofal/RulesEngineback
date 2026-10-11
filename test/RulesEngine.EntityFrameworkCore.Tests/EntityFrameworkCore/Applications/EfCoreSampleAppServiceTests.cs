using RulesEngine.Samples;
using Xunit;

namespace RulesEngine.EntityFrameworkCore.Applications;

[Collection(RulesEngineTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<RulesEngineEntityFrameworkCoreTestModule>
{

}
