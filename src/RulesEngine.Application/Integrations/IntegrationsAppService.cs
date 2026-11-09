using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using RulesEngine.Permissions;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Threading;

namespace RulesEngine.Integrations
{
 //   [Authorize(RulesEnginePermissions.Validation)]
    public class IntegrationsAppService : ApplicationService, IIntegrationsAppService
    {
        private readonly IRulesValidationManager _rulesValidationManager;
 
        public IntegrationsAppService(IRulesValidationManager rulesValidationManager )
        {
            _rulesValidationManager = rulesValidationManager;
         }

        public async Task<ClaimValidationResponse> ValidateClaimsIntegrationExtended(ClaimValidation input, CancellationToken cancellationToken = default)
        {
            if ((input.workflows == null || input.workflows.Count == 0) && (input.workflownames == null || input.workflownames.Count == 0))
                throw new UserFriendlyException("Workflows are required (either workflows ids or workflownames)");

            if (input.ClaimValidationRequests == null || input.ClaimValidationRequests.Count == 0)
                throw new UserFriendlyException("At least one claim is required");

            // Delegate to existing validation manager which implements the detailed rules engine logic.
            var result = await _rulesValidationManager.SpecificValidateClaimsByCodesExtended(
                input.workflows,
                input.workflownames,
                input.ClaimValidationRequests, cancellationToken
                );
            return result;
        }
    }
}
