using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
namespace RulesEngine.CustomAppServices;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityUserAppService), typeof(IdentityUserAppService), typeof(CustomUserAppService))]
public class CustomUserAppService : IdentityUserAppService
{
    private readonly IOrganizationUnitRepository _organizationUnitRepository;
    private readonly ILogger<CustomUserAppService> _logger;

    public CustomUserAppService(IdentityUserManager userManager, IIdentityUserRepository userRepository, IIdentityRoleRepository roleRepository, IOptions<IdentityOptions> identityOptions, IPermissionChecker permissionChecker, IOrganizationUnitRepository organizationUnitRepository, ILogger<CustomUserAppService> logger) : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _logger = logger;
    }

    public override async Task<IdentityUserDto> CreateAsync(IdentityUserCreateDto input)
    {
        var organizationId = input.ExtraProperties["OrganizationId"]?.ToString();
        var organization = await _organizationUnitRepository.GetAsync(organizationId);
        input.ExtraProperties["OrganizationId"] = organization.DisplayName;
        var user = await base.CreateAsync(input);




        await UserManager.AddToOrganizationUnitAsync(user.Id, organization.Id);



        return user;
    }



    public override async Task<IdentityUserDto> UpdateAsync(Guid id, IdentityUserUpdateDto input)
    {
        var user = await base.GetAsync(id);
        if (user == null)
        {
            throw new UserFriendlyException("User not found");
        }

        var oldOrganizationName = user.ExtraProperties["OrganizationId"]?.ToString();
        var oldOrganization = await _organizationUnitRepository.GetAsync(oldOrganizationName);

        var organizationName = input.ExtraProperties["OrganizationId"]?.ToString();
        var organization = await _organizationUnitRepository.GetAsync(organizationName);
        if (oldOrganizationName != organizationName && !string.IsNullOrEmpty(oldOrganizationName))
        {
            await UserManager.RemoveFromOrganizationUnitAsync(user.Id, oldOrganization.Id);
            await UserManager.AddToOrganizationUnitAsync(user.Id, organization.Id);
        }
        var userdto = await base.UpdateAsync(id, input);

        return userdto;
    }

    public override async Task<PagedResultDto<IdentityUserDto>> GetListAsync(GetIdentityUsersInput input)
    {
        input.Sorting = input.Sorting ?? "CreationTime desc";
        return await base.GetListAsync(input);
    }

}

