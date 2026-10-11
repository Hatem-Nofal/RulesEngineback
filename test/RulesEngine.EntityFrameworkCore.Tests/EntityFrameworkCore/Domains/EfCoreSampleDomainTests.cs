using RulesEngine.Samples;
using Xunit;

namespace RulesEngine.EntityFrameworkCore.Domains;

[Collection(RulesEngineTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<RulesEngineEntityFrameworkCoreTestModule>
{

}
