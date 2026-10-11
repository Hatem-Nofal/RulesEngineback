using System.Threading.Tasks;

namespace RulesEngine.Data;

public interface IRulesEngineDbSchemaMigrator
{
    Task MigrateAsync();
}
