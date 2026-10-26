using Microsoft.EntityFrameworkCore;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.EntityFrameworkCore;
using RulesEngine.Repositories.WorkFlows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RulesEngine.WorkFlows.Repositories.WorkFlows
{



    public class WorkFlowRepository
: EfCoreRepository<RulesEngineDbContext, WorkFlow, Guid>,
IWorkFlowRepository
    {
        private readonly RulesEngineDbContext _context;

        public WorkFlowRepository(IDbContextProvider<RulesEngineDbContext> dbContextProvider, RulesEngineDbContext context) : base(dbContextProvider)
        {
            _context = context;
        }

        public async Task<List<WorkFlow>> GetListAsync()
        {
            return await _context.WorkFlows.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<WorkFlow> GetAsync(Guid id)
        {
            var w = await _context.WorkFlows
                //.Include(x=>x.Rules)
                //.Include(x=>x.Rules).ThenInclude(x=>x.Nodes)
                .Include(x => x.RuleformConfig)
                .ThenInclude(x => x.RuleFormConfigFields)
                //   .ThenInclude(x=>x.Rulefield)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException("Workflow not found.");

            return w;
        }

        public async Task<Guid> CreateAsync(WorkFlow input)
        {
            await ValidateAsync(input, null);

            _context.WorkFlows.Add(input);
            await _context.SaveChangesAsync();
            return input.Id;
        }

        public async Task UpdateAsync(Guid id, WorkFlow input)
        {
            var w = await _context.WorkFlows.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException("Workflow not found.");
            await ValidateAsync(input, id);
            w.UpdateName(input.Name.Trim());
            w.UpdateRuleFormConfigId(input.RuleformConfigId);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var count = await _context.Rules.CountAsync(x => x.WorkflowId == id);
            if (count > 0)
                throw new RuleValidationException($"Cannot delete: {count} rule(s) use this workflow. Delete or move them first.");

            var deleted = await _context.WorkFlows.Where(x => x.Id == id).ExecuteDeleteAsync();
            if (deleted == 0) throw new KeyNotFoundException("Workflow not found.");
        }

        private async Task ValidateAsync(WorkFlow input, Guid? currentId)
        {
            var name = input.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new RuleValidationException("Workflow name is required.");
            if (name.Length > 200)
                throw new RuleValidationException("Workflow name is too long (max 200).");

            // Plain equality keeps the unique index on Name usable (SQL Server's default collation is case-insensitive anyway).
            if (await _context.WorkFlows.AnyAsync(x => x.Id != currentId && x.Name == name))
                throw new RuleValidationException($"A workflow named '{name}' already exists.");

            if (input.RuleformConfigId is { } cfgId
                && !await _context.RuleFormConfigs.AnyAsync(x => x.Id == cfgId && x.IsActive))
                throw new RuleValidationException("The selected form config does not exist or is inactive.");
        }

        public async Task CreateRulesAsync(WorkFlow input)
        {
            var w = await _context.WorkFlows

                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.Id)
                    ?? throw new KeyNotFoundException("Workflow not found.");
            w = input;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Read-only source for the rules engine snapshot: no change tracking, and inactive rules are filtered in SQL
        /// (the engine skips disabled rules anyway, so they would only cost transfer and memory).
        /// </summary>
        public Task<IQueryable<WorkFlow>> GetAllForRuleEngine()
        {
            IQueryable<WorkFlow> query = _context.WorkFlows
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Rules.Where(r => r.IsActive));

            return Task.FromResult(query);
        }

        public async Task<RulesFingerprint> GetRulesFingerprintAsync(CancellationToken cancellationToken = default)
        {
            var workflows = await _context.WorkFlows.AsNoTracking()
                .Where(x => x.IsActive)
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Last = g.Max(x => x.LastModificationTime ?? x.CreationTime) })
                .FirstOrDefaultAsync(cancellationToken);

            var rules = await _context.Rules.AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Last = g.Max(x => x.LastModificationTime ?? x.CreationTime) })
                .FirstOrDefaultAsync(cancellationToken);

            DateTime? last = workflows is null ? rules?.Last
                : rules is null ? workflows.Last
                : (workflows.Last > rules.Last ? workflows.Last : rules.Last);

            return new RulesFingerprint(workflows?.Count ?? 0, rules?.Count ?? 0, last);
        }
    }
}
