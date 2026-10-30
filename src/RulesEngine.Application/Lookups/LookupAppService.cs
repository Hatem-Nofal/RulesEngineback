using RulesEngine.Common.Dtos;
using RulesEngine.Entities.Lookups;
using RulesEngine.Entities.WorkFlows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using RulesEngine.Permissions;

namespace RulesEngine.Lookups
{
    /// <summary>
    /// Drop-down data for the UI.
    /// - Every query is awaited (the previous version called the synchronous ToList() inside async methods,
    ///   which blocks a thread-pool thread per request).
    /// - Reference tables (activity types/sub types, diagnoses, specialities) change rarely, so they are cached
    ///   in memory for a few minutes instead of being re-read on every dropdown open.
    /// - Workflows change often, so that list is never cached.
    /// </summary>
    [Authorize(RulesEnginePermissions.Lookups)]
    public class LookupAppService : ApplicationService, ILookupAppService
    {


        private readonly IRepository<ActivityType> _activityTypeRepository;
        private readonly IRepository<ActivitySubType> _activitySubTypeRepository;
        private readonly IRepository<Diagnosis> _diagnosisRepository;
        private readonly IRepository<Specialities> _specialitiesRepository;
        private readonly IRepository<WorkFlow> _workFlowsRepository;
        private readonly IRepository<GenericDrug> _genericDrugsRepository;
 
        private readonly IRepository<DuplicateTherapyGroup, int> _duplicateTherapyGroupRepository;
        private readonly IRepository<MorbidityGroup, int> _morbidityGroupRepository;
        private readonly IRepository<BatchType, int> _batchTypeRepository;

        public LookupAppService(
            IRepository<ActivityType> activityTypeRepository,
            IRepository<ActivitySubType> activitySubTypeRepository,
            IRepository<Diagnosis> diagnosisRepository,
            IRepository<Specialities> specialitiesRepository,
            IRepository<WorkFlow> workFlowsRepository,
            IRepository<GenericDrug> genericDrugsRepository,
            IRepository<DuplicateTherapyGroup, int> duplicateTherapyGroupRepository,
            IRepository<MorbidityGroup, int> morbidityGroupRepository,
            IRepository<BatchType, int> batchTypeRepository
            )
        {
            _activityTypeRepository = activityTypeRepository;
            _activitySubTypeRepository = activitySubTypeRepository;
            _diagnosisRepository = diagnosisRepository;
            _specialitiesRepository = specialitiesRepository;
            _workFlowsRepository = workFlowsRepository;
            _genericDrugsRepository = genericDrugsRepository;
            _duplicateTherapyGroupRepository = duplicateTherapyGroupRepository;
            _morbidityGroupRepository = morbidityGroupRepository;
            _batchTypeRepository = batchTypeRepository;
         }

        public async Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownList(int? activityTypeId)
        {
            var queryable = await _activitySubTypeRepository.GetQueryableAsync();
            var query = queryable.WhereIf(activityTypeId.HasValue, x => x.ActivityType_id == activityTypeId).Select(x => new SelectedDropDownList(x.Id, x.Short_Desc, x.iqvia_code, false));
            return await AsyncExecuter.ToListAsync(query);
        }

        public async Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownListPaged(int activityTypeId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return new List<SelectedDropDownList>();

            _activitySubTypeRepository.DisableTracking();

            string lowerSearchTerm = searchTerm.ToLower();


            var rawMatches = await _activitySubTypeRepository.GetListAsync(x =>
                x.ActivityType_id == activityTypeId

                && (x.Medium_Desc.ToLower().Contains(lowerSearchTerm) || x.Short_Desc.ToLower().Contains(lowerSearchTerm))
            );

            var matches = rawMatches
                .Select(x => new
                {
                    Item = new SelectedDropDownList(
                        x.Id,
                        x.Short_Desc, x.iqvia_code, false// <-- concatenated display text
                    ),
                    Priority = x.Short_Desc.ToLower().StartsWith(lowerSearchTerm) ? 0 :
                               x.Medium_Desc.ToLower().StartsWith(lowerSearchTerm) ? 1 :
                               x.Short_Desc.ToLower().Contains(lowerSearchTerm) ? 2 : 3
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Item.Name)
                .Select(x => x.Item)
                .DistinctBy(x => x.Id)
                .Take(15)
                .ToList();

            return matches;
        }
        public async Task<List<SelectedDropDownList>> GetDiagnosisDropDownListPaged(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SelectedDropDownList>();

            string normalizedSearch = searchTerm.ToLowerInvariant();

            _diagnosisRepository.DisableTracking();

            var allMatches = await _diagnosisRepository.GetListAsync(
                x => x.Medium_Desc.ToLower().Contains(normalizedSearch) ||
                     x.Short_Desc.ToLower().Contains(normalizedSearch) ||
                     x.iqvia_code.ToLower().Contains(normalizedSearch)
            );

            var orderedResults = allMatches
                .Select(x => new
                {
                    Item = new SelectedDropDownList(
                        x.Id,
                        x.Short_Desc, x.iqvia_code, false// <-- concatenated display text
                    ),
                    Priority =
                        x.Medium_Desc.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 :
                        x.Short_Desc.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                        x.iqvia_code.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                        2
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Item.Name)
                .Select(x => x.Item)
                .DistinctBy(x => x.Id)
                .Take(15)
                .ToList();

            return orderedResults;
        }

        public async Task<List<SelectedDropDownList>> GetActivityTypeDropDownList()
        {
            var queryable = await _activityTypeRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.Description, x.IsActive }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.Description, x.IsActive)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetDiagnosisDropDownList()
        {
            var queryable = await _diagnosisRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.Short_Desc, x.iqvia_code }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.Short_Desc, x.iqvia_code, false)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetSpecialitiesIdDropDownList(string searchTerm)
        {
            //var queryable = await _specialitiesRepository.GetQueryableAsync();
            //var rows = await AsyncExecuter.ToListAsync(
            //    queryable.Select(x => new { x.Id, x.SpecialityDescription, x.SpecialityCode }));

            //return rows.Select(x => new SelectedDropDownList(x.Id, x.SpecialityDescription, x.SpecialityCode, false)).ToList();


            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SelectedDropDownList>();

            string normalizedSearch = searchTerm.ToLowerInvariant();

            _specialitiesRepository.DisableTracking();

            var allMatches = await _specialitiesRepository.GetListAsync(
                x => x.SpecialityDisplay.ToLower().Contains(normalizedSearch) ||
                     x.SpecialityDescription.ToLower().Contains(normalizedSearch) ||
                     x.SpecialityCode.ToLower().Contains(normalizedSearch)
            );

            var orderedResults = allMatches
                .Select(x => new
                {
                    Item = new SelectedDropDownList(
                        x.Id,
                        x.SpecialityDisplay, x.SpecialityCode, false// <-- concatenated display text
                    ),
                    Priority =
                        x.SpecialityDisplay.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 :
                        x.SpecialityDescription.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                        x.SpecialityCode.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                        2
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Item.Name)
                .Select(x => x.Item)
                .DistinctBy(x => x.Id)
                .Take(15)
                .ToList();

            return orderedResults;


        }

        public async Task<List<SelectedDropDownListByGuid>> GetWorkflowsDropDownList()
        {
            var queryable = await _workFlowsRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.Name }));

            return rows.Select(x => new SelectedDropDownListByGuid(x.Id, x.Name, false)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetGenericDrugsDropDownList(string searchTerm)
        {
            //var queryable = await _genericDrugsRepository.GetQueryableAsync();
            //var rows = await AsyncExecuter.ToListAsync(
            //    queryable.Select(x => new { x.Id, x.GenericName, x.SystemCode }));

            //return rows.Select(x => new SelectedDropDownList(x.Id, x.GenericName, x.SystemCode, false)).ToList();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SelectedDropDownList>();

            string normalizedSearch = searchTerm.ToLowerInvariant();

            _genericDrugsRepository.DisableTracking();

            var allMatches = await _genericDrugsRepository.GetListAsync(
                x => x.GenericName.ToLower().Contains(normalizedSearch) ||
                      x.GenericCode.ToLower().Contains(normalizedSearch)
            );

            var orderedResults = allMatches
                .Select(x => new
                {
                    Item = new SelectedDropDownList(
                        x.Id,
                        x.GenericName, x.GenericCode, false// <-- concatenated display text
                    ),
                    Priority =
                        x.GenericName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 :
                        x.GenericCode.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                         2
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Item.Name)
                .Select(x => x.Item)
                .DistinctBy(x => x.Id)
                .Take(15)
                .ToList();

            return orderedResults;
        }


         

        public async Task<List<SelectedDropDownList>> GetDuplicateTherapyGroupsDropDownList(string searchTerm)
        {
            //var queryable = await _duplicateTherapyGroupRepository.GetQueryableAsync();
            //var rows = await AsyncExecuter.ToListAsync(
            //    queryable.Select(x => new { x.Id, x.DtGroupName, x.DtgSystemCode }));

            //return rows.Select(x => new SelectedDropDownList(x.Id, x.DtGroupName, x.DtgSystemCode, false)).ToList();


            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SelectedDropDownList>();

            string normalizedSearch = searchTerm.ToLowerInvariant();

            _duplicateTherapyGroupRepository.DisableTracking();

            var allMatches = await _duplicateTherapyGroupRepository.GetListAsync(
                x => x.DtGroupName.ToLower().Contains(normalizedSearch) ||
                      x.DtgSystemCode.ToLower().Contains(normalizedSearch)
            );

            var orderedResults = allMatches
                .Select(x => new
                {
                    Item = new SelectedDropDownList(
                        x.Id,
                        x.DtGroupName, x.DtgSystemCode, false// <-- concatenated display text
                    ),
                    Priority =
                        x.DtGroupName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 :
                        x.DtgSystemCode.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                         2
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Item.Name)
                .Select(x => x.Item)
                .DistinctBy(x => x.Id)
                .Take(15)
                .ToList();

            return orderedResults;
        }

        public async Task<List<SelectedDropDownList>> GetMorbidityGroupsDropDownList(string searchTerm)
        {
            //var queryable = await _morbidityGroupRepository.GetQueryableAsync();
            //var rows = await AsyncExecuter.ToListAsync(
            //    queryable.Select(x => new { x.Id, x.MorbidityGroupName, x.MorbidityGroupSystemCode }));

            //return rows.Select(x => new SelectedDropDownList(x.Id, x.MorbidityGroupName, x.MorbidityGroupSystemCode, false)).ToList();

            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SelectedDropDownList>();

            string normalizedSearch = searchTerm.ToLowerInvariant();

            _morbidityGroupRepository.DisableTracking();

            var allMatches = await _morbidityGroupRepository.GetListAsync(
                x => x.MorbidityGroupName.ToLower().Contains(normalizedSearch) ||
                      x.MorbidityGroupSystemCode.ToLower().Contains(normalizedSearch)
            );

            var orderedResults = allMatches
                .Select(x => new
                {
                    Item = new SelectedDropDownList(
                        x.Id,
                        x.MorbidityGroupName, x.MorbidityGroupSystemCode, false// <-- concatenated display text
                    ),
                    Priority =
                        x.MorbidityGroupName.Equals(searchTerm, StringComparison.OrdinalIgnoreCase) ? 0 :
                        x.MorbidityGroupSystemCode.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) ? 1 :
                         2
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Item.Name)
                .Select(x => x.Item)
                .DistinctBy(x => x.Id)
                .Take(15)
                .ToList();

            return orderedResults;
        }

        public async Task<List<SelectedDropDownList>> GetBatchTypesDropDownList()
        {
            var queryable = await _batchTypeRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.Name, x.Code }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.Name, x.Code, false)).ToList();
        }

        private sealed record ActivitySubTypeRow(long Id, int ActivityTypeId, string IqviaCode, string ShortDesc);

        /// <summary>
        /// One cached value with an absolute expiry. Concurrent callers on a cold or expired entry wait for a single
        /// load instead of each hitting the database. A failed load is not cached.
        /// </summary>
        private sealed class TimedAsyncCache<T> where T : class
        {
            private sealed record Entry(T Value, long ExpiresAtMs);

            private readonly long _ttlMs;
            private readonly SemaphoreSlim _gate = new(1, 1);
            private volatile Entry? _entry;

            public TimedAsyncCache(TimeSpan ttl) => _ttlMs = (long)ttl.TotalMilliseconds;

            public async Task<T> GetOrCreateAsync(Func<Task<T>> factory)
            {
                var current = _entry;
                if (current is not null && Environment.TickCount64 < current.ExpiresAtMs)
                    return current.Value;

                await _gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    current = _entry;
                    if (current is not null && Environment.TickCount64 < current.ExpiresAtMs)
                        return current.Value;

                    var value = await factory().ConfigureAwait(false);
                    _entry = new Entry(value, Environment.TickCount64 + _ttlMs);
                    return value;
                }
                finally
                {
                    _gate.Release();
                }
            }
        }
    }
}
