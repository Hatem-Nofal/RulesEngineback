using RulesEngine.Localization;
using Volo.Abp.Application.Services;

namespace RulesEngine;

/* Inherit your application services from this class.
 */
public abstract class RulesEngineAppService : ApplicationService
{
    protected RulesEngineAppService()
    {
        LocalizationResource = typeof(RulesEngineResource);
    }
}
