using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using RulesEngine.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace RulesEngine.CustomAppServices;

[Microsoft.AspNetCore.Authorization.Authorize("AbpIdentity.Roles")]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityRoleAppService), typeof(IdentityRoleAppService), typeof(CustomRolesAppService))]
public class CustomRolesAppService : IdentityRoleAppService
{
    private readonly IStringLocalizer<RulesEngineResource> _localizer;
    public CustomRolesAppService(IdentityRoleManager roleManager, IIdentityRoleRepository roleRepository, IStringLocalizer<RulesEngineResource> localizer) : base(roleManager, roleRepository)
    {
        _localizer = localizer;
    }
    public override async Task DeleteAsync(Guid id)
    {
        var role = await RoleManager.FindByIdAsync(id.ToString());

        if (role == null)
            return;

        var roleWithUsers = await RoleRepository.GetListWithUserCountAsync(null, int.MaxValue, 0, role.Name);

        if (roleWithUsers.Any() && roleWithUsers.Any(x => x.UserCount > 0))
            throw new UserFriendlyException(_localizer["PreventDeleteRole"]);


        (await RoleManager.DeleteAsync(role)).CheckErrors();
    }
}

