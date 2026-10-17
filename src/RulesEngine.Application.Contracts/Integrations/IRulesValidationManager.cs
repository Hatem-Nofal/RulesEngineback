using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Integrations
{
    public interface IRulesValidationManager
    {
        Task<ClaimValidationResponse> SpecificValidateClaimsByCodesExtended(List<Guid>? workflowIds, List<string>? workflownames, List<ClaimValidationByCodeRequestExtended> claims);

    }
}
