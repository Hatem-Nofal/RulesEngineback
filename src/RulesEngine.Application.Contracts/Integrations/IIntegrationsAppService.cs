using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace RulesEngine.Integrations
{
    public interface IIntegrationsAppService: IApplicationService
    {
        Task<ClaimValidationResponse> ValidateClaimsIntegrationExtended(ClaimValidation input);
    }
}
