using RulesEngine.Common.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace EhealthClaims.Lookups
{
    public interface ILookupAppService : IApplicationService
    {
        Task<List<SelectedDropDownList>> GetActivityTypeDropDownList();
        Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownList(int? activityTypeId);
        Task<List<SelectedDropDownList>> GetDiagnosisDropDownList();
        Task<List<SelectedDropDownList>> GetSpecialitiesIdDropDownList();
        Task<List<SelectedDropDownListByGuid>> GetWorkflowsDropDownList();

    }
}
