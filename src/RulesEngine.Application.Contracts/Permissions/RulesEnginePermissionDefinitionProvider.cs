using RulesEngine.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace RulesEngine.Permissions;

public class RulesEnginePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(RulesEnginePermissions.GroupName);
        myGroup.AddPermission(RulesEnginePermissions.Lookups, L("Permission:RulesEngineLookups"));
        myGroup.AddPermission(RulesEnginePermissions.Validation, L("Permission:RulesEngineValidation"));
        myGroup.AddPermission(RulesEnginePermissions.Workflows, L("Permission:RulesEngineWorkflows"));
        myGroup.AddPermission(RulesEnginePermissions.Rules, L("Permission:RulesEngineRules"));
        myGroup.AddPermission(RulesEnginePermissions.RuleFormConfigs, L("Permission:RulesEngineRuleFormConfigs"));
        myGroup.AddPermission(RulesEnginePermissions.Administration, L("Permission:RulesEngineAdministration"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RulesEngineResource>(name);
    }
}
