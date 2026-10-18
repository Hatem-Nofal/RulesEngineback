using Microsoft.Extensions.Logging;
using RulesEngine.Integrations.RulesEngineManagers;
using RulesEngine.IntegrationsNew;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace RulesEngine.Integrations
{
    //public class IntegrationsAppService : ApplicationService, IIntegrationsAppService
    //{
    //    private readonly IRulesValidationManager _rulesValidationManager;
    //    private readonly ILogger<IntegrationsAppService> _logger;

    //    public IntegrationsAppService(IRulesValidationManager rulesValidationManager, ILogger<IntegrationsAppService> logger)
    //    {
    //        _rulesValidationManager = rulesValidationManager;
    //        _logger = logger;
    //    }

    //    public async Task<ClaimValidationResponse> ValidateClaimsIntegrationExtended(ClaimValidation input)
    //    {
    //        if ((input.workflows == null || input.workflows.Count == 0) && (input.workflownames == null || input.workflownames.Count == 0))
    //            throw new UserFriendlyException("Workflows are required (either workflows ids or workflownames)");

    //        if (input.ClaimValidationRequests == null || input.ClaimValidationRequests.Count == 0)
    //            throw new UserFriendlyException("At least one claim is required");

    //        // Delegate to existing validation manager which implements the detailed rules engine logic.
    //        var result = await _rulesValidationManager.SpecificValidateClaimsByCodesExtended(input.workflows, input.workflownames, input.ClaimValidationRequests);
    //        return result;
    //    }
    //}
}
