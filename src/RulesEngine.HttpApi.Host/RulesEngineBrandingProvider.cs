using Microsoft.Extensions.Localization;
using RulesEngine.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace RulesEngine;

[Dependency(ReplaceServices = true)]
public class RulesEngineBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<RulesEngineResource> _localizer;

    public RulesEngineBrandingProvider(IStringLocalizer<RulesEngineResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
