using RulesEngine.Common.Dtos;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace EhealthClaims.Lookups
{
    /// <summary>
    /// Drop-down data for the UI.
    /// - Every query is awaited (the previous version called the synchronous ToList() inside async methods,
    ///   which blocks a thread-pool thread per request).
    /// - Reference tables (activity types/sub types, diagnoses, specialities) change rarely, so they are cached
    ///   in memory for a few minutes instead of being re-read on every dropdown open.
    /// - Workflows change often, so that list is never cached.
    /// </summary>
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

        public LookupAppService(
            IRepository<ActivityType> activityTypeRepository,
            IRepository<ActivitySubType> activitySubTypeRepository,
            IRepository<Diagnosis> diagnosisRepository,
            IRepository<Specialities> specialitiesRepository,
            IRepository<WorkFlow> workFlowsRepository,
            IRepository<GenericDrug> genericDrugsRepository,
            IRepository<DuplicateTherapyGroup, int> duplicateTherapyGroupRepository,
            IRepository<MorbidityGroup, int> morbidityGroupRepository)
        {
            _activityTypeRepository = activityTypeRepository;
            _activitySubTypeRepository = activitySubTypeRepository;
            _diagnosisRepository = diagnosisRepository;
            _specialitiesRepository = specialitiesRepository;
            _workFlowsRepository = workFlowsRepository;
            _genericDrugsRepository = genericDrugsRepository;
            _duplicateTherapyGroupRepository = duplicateTherapyGroupRepository;
            _morbidityGroupRepository = morbidityGroupRepository;
        }

        public async Task<List<SelectedDropDownList>> GetActivitySubTypeDropDownList(int? activityTypeId)
        {
            var queryable = await _activitySubTypeRepository.GetQueryableAsync();
            var query = queryable.WhereIf(activityTypeId.HasValue,x=>x.ActivityType_id==activityTypeId).Select(x => new SelectedDropDownList(x.Id, x.iqvia_code, x.Short_Desc, false));
            return await AsyncExecuter.ToListAsync(query);
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

        public async Task<List<SelectedDropDownList>> GetSpecialitiesIdDropDownList()
        {
            var queryable = await _specialitiesRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.Description, x.Code }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.Description, x.Code, false)).ToList();
        }

        public async Task<List<SelectedDropDownListByGuid>> GetWorkflowsDropDownList()
        {
            var queryable = await _workFlowsRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.Name }));

            return rows.Select(x => new SelectedDropDownListByGuid(x.Id, x.Name, false)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetGenericDrugsDropDownList()  
        {
            var queryable = await _genericDrugsRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.GenericName, x.SystemCode }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.GenericName, x.SystemCode, false)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetProcedureGroupsDropDownList()  
        {
            var queryable = await _genericDrugsRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.GenericName, x.SystemCode }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.GenericName, x.SystemCode, false)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetDuplicateTherapyGroupsDropDownList()  
        {
            var queryable = await _duplicateTherapyGroupRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.DtGroupName, x.DtgSystemCode }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.DtGroupName, x.DtgSystemCode, false)).ToList();
        }

        public async Task<List<SelectedDropDownList>> GetMorbidityGroupsDropDownList()  
        {
            var queryable = await _morbidityGroupRepository.GetQueryableAsync();
            var rows = await AsyncExecuter.ToListAsync(
                queryable.Select(x => new { x.Id, x.MorbidityGroupName, x.MorbidityGroupSystemCode }));

            return rows.Select(x => new SelectedDropDownList(x.Id, x.MorbidityGroupName, x.MorbidityGroupSystemCode, false)).ToList();
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
