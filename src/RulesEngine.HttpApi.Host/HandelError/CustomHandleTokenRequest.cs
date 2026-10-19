using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using RulesEngine.Localization;
using System;
 using System.Threading.Tasks;
using static OpenIddict.Server.OpenIddictServerEvents;
 
namespace RulesEngine.HandelError;

public class CustomHandleTokenRequest : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    private readonly IStringLocalizer<RulesEngineResource> _localizer;

    public CustomHandleTokenRequest(IStringLocalizer<RulesEngineResource> localizer)
    {
        _localizer = localizer;
    }

    public ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        var response = context.Response;
        if (string.Equals(response.Error, OpenIddictConstants.Errors.InvalidGrant, StringComparison.Ordinal) &&
           !string.IsNullOrEmpty(response.ErrorDescription))
        {
            response.ErrorDescription = _localizer["InvalidGrant"];
        }

        return default;
    }
}