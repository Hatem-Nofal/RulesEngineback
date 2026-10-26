using RulesEngine.Common.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RulesEngine.Lookups
{
    public interface ILookupAppService
    {
        Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownList(int? activityType);
        Task<List<SelectedDropDownList>> GetActivityTypeDropDownList();
        Task<List<SelectedDropDownList>> GetDiagnosisDropDownList();
         Task<List<SelectedDropDownList>> GetSpecialitiesIdDropDownList(string searchTerm);
        Task<List<SelectedDropDownListByGuid>> GetWorkflowsDropDownList();
        Task<List<SelectedDropDownList>> GetGenericDrugsDropDownList(string searchTerm);
         Task<List<SelectedDropDownList>> GetDuplicateTherapyGroupsDropDownList(string searchTerm);
        Task<List<SelectedDropDownList>> GetMorbidityGroupsDropDownList(string searchTerm);
        Task<List<SelectedDropDownList>> GetBatchTypesDropDownList();
        Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownListPaged(int activityTypeId, string searchTerm);
        Task<List<SelectedDropDownList>> GetDiagnosisDropDownListPaged(string searchTerm);
      }
}