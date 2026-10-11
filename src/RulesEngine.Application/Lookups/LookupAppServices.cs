using RulesEngine.Common.Dtos;
using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace EhealthClaims.Lookups
{
    public class LookupAppServices : ApplicationService, ILookupServices
    {
        private readonly IRepository<ActivityType> _activityTypeRepository;
        private readonly IRepository<ActivitySubType> _activitySubTypeRepository;

        private readonly IRepository<Diagnosis> _diagnosisRepository;
        private readonly IRepository<Specialities> _specialitiesRepository;

        public LookupAppServices(IRepository<ActivityType> activityTypeRepository, IRepository<ActivitySubType> activitySubTypeRepository, IRepository<Diagnosis> diagnosisRepository, IRepository<Specialities> specialitiesRepository)
        {
            _activityTypeRepository = activityTypeRepository;
            _activitySubTypeRepository = activitySubTypeRepository;
            _diagnosisRepository = diagnosisRepository;
            _specialitiesRepository = specialitiesRepository;
        }

        public async Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownList(int? activityTypeId)
        {
            var queryable = await _activitySubTypeRepository.GetQueryableAsync();

            var entities = queryable.WhereIf(activityTypeId.HasValue, x => x.ActivityTypeId == activityTypeId.Value)
                .Select(x => new SelectedDropDownList(x.Id, x.ShortDesc, x.IqviaCode, false))
                .ToList();

            return entities.Select(x => new SelectedDropDownList(x.Id,x.Code, x.Name,false)).ToList();
        }

        public  async Task<List<SelectedDropDownList>> GetActivityTypeDropDownList()
        {
            var queryable = await _activityTypeRepository.GetQueryableAsync();

            return queryable.Select(x => new SelectedDropDownList(x.Id, x.Description, x.IsActive)).ToList();
        }

        public  async Task<List<SelectedDropDownList>> GetDiagnosisDropDownList()
        {
            var queryable = await _diagnosisRepository.GetQueryableAsync();

            return queryable.Select(x => new SelectedDropDownList(x.Id, x.ShortDesc,x.IqviaCode,false)).ToList();
        }

        public  async Task<List<SelectedDropDownList>> GetSpecialitiesIdDropDownList()
        {
            var queryable = await _specialitiesRepository.GetQueryableAsync();

            return queryable.Select(x => new SelectedDropDownList(x.Id, x.Description, x.Code, false)).ToList();
        }
    }
}
