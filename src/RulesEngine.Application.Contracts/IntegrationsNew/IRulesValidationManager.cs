using RulesEngine.Integrations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RulesEngine.IntegrationsNew
{
    public interface IRulesValidationManager
    {
        Task<ClaimValidationResponse> SpecificValidateClaimsByCodesExtended(List<Guid>? workflowIds, List<string>? workflownames, List<ClaimValidationByCodeRequestExtended> claims, CancellationToken cancellationToken = default);

    }
}
