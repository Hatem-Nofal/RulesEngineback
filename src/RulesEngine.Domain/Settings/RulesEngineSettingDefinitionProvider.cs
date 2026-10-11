using Volo.Abp.Settings;

namespace RulesEngine.Settings;

public class RulesEngineSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(RulesEngineSettings.MySetting1));
    }
}
