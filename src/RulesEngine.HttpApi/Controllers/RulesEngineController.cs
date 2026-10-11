using RulesEngine.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace RulesEngine.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class RulesEngineController : AbpControllerBase
{
    protected RulesEngineController()
    {
        LocalizationResource = typeof(RulesEngineResource);
    }
}
