using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace RulesEngine.Data;

/* This is used if database provider does't define
 * IRulesEngineDbSchemaMigrator implementation.
 */
public class NullRulesEngineDbSchemaMigrator : IRulesEngineDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
