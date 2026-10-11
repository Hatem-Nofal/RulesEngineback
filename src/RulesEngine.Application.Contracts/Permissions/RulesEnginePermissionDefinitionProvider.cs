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

        //Define your own permissions here. Example:
        //myGroup.AddPermission(RulesEnginePermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<RulesEngineResource>(name);
    }
}
