using Microsoft.EntityFrameworkCore;
using RulesEngine.Application.Repositories.WorkFlows;
using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace RulesEngine.WorkFlows.Repositories.WorkFlows
{
    public class RuleFormConfigRepository : EfCoreRepository<RulesEngineDbContext, RuleFormConfig, Guid>, IRuleFormConfigRepository
    {
        private readonly RulesEngineDbContext _context;
        public RuleFormConfigRepository(IDbContextProvider<RulesEngineDbContext> dbContextProvider, RulesEngineDbContext context) : base(dbContextProvider)
        {
            _context = context;
        }

        public async Task<List<RuleFormConfig>> GetConfigsAsync()
        {
            return await _context.RuleFormConfigs.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                 .ToListAsync();
        }

  

        public async Task<RuleFormConfig?> GetByWorkflowAsync(Guid workflowId)
        {
            var workflow = await _context.WorkFlows.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workflowId)
                           ?? throw new KeyNotFoundException("Workflow not found.");

            var configId = workflow.RuleformconfigId;
            var config = await _context.RuleFormConfigs.AsNoTracking().Include(x => x.RuleFormConfigFields)
                .FirstOrDefaultAsync(x => x.Id == configId && x.IsActive);
            if (config is null) return null;

            var ids = config.RuleFormConfigFields.Select(x => x.RulefieldId).ToList();
            var catalog = await _context.RuleFields.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            return config;


        }

        public async Task<List<RuleFormConfig>> GetListAsync()
        {
            var fieldCountRows = await _context.RuleFormConfigFields.AsNoTracking()
                .GroupBy(x => x.RuleformConfigId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();
            var fieldCounts = fieldCountRows.ToDictionary(x => x.Id, x => x.Count);

            var workflowRows = await _context.WorkFlows.AsNoTracking()
                .Where(x => x.RuleformconfigId != null)
                .GroupBy(x => x.RuleformconfigId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();
            var workflowCounts = workflowRows.ToDictionary(x => x.Id, x => x.Count);

            var configs = await _context.RuleFormConfigs.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
            //return configs.Select(c => new FormConfigListDto
            //{
            //    Id = c.Id,
            //    Name = c.Name,
            //    Description = c.Description,
            //    IsActive = c.IsActive,
            //    AllowNestedGroups = c.AllowNestedGroups,
            //    StrategyKey = c.StrategyKey,
            //    FieldCount = fieldCounts.GetValueOrDefault(c.Id),
            //    WorkflowCount = workflowCounts.GetValueOrDefault(c.Id)
            //}).ToList();
            return configs;
        }

        public async Task<RuleFormConfig> GetAsync(Guid id)
        {
            var config = await _context.RuleFormConfigs.AsNoTracking().Include(x => x.RuleFormConfigFields).ThenInclude(x=>x.Rulefield)
                        .FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new KeyNotFoundException("Form config not found.");

            //return new FormConfigEditDto
            //{
            //    Id = c.Id,
            //    Name = c.Name,
            //    Description = c.Description,
            //    IsActive = c.IsActive,
            //    AllowNestedGroups = c.AllowNestedGroups,
            //    Fields = c.Fields.OrderBy(x => x.Order).Select(x => new FormConfigFieldInputDto
            //    {
            //        FieldId = x.RuleFieldId,
            //        AllowedOperators = x.AllowedOperatorsJson is null
            //            ? null
            //            : JsonSerializer.Deserialize<List<int>>(x.AllowedOperatorsJson)
            //    }).ToList()
            //};
             return config;
        }

        public async Task<RuleFormConfig> CreateAsync(RuleFormConfig input)
        {
            var catalog = await ValidateAsync(input, null);

            var config = new RuleFormConfig(  input.Name?.Trim(), input.Description?.Trim());
  

            var fields = BuildFields(input, catalog);
            config.RuleFormConfigFields.AddRange(fields);

            _context.RuleFormConfigs.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }

        public async Task<RuleFormConfig> UpdateAsync(Guid id, RuleFormConfig input)
        {
            var config = await _context.RuleFormConfigs.FirstOrDefaultAsync(x => x.Id == id)
                         ?? throw new KeyNotFoundException("Form config not found.");
            var catalog = await ValidateAsync(input, id);

            var oldFieldIds = await _context.RuleFormConfigFields.Where(x => x.RuleformConfigId == id)
                .Select(x => x.RulefieldId).ToListAsync();
            var oldKeys = await _context.RuleFields.Where(f => oldFieldIds.Contains(f.Id)).Select(f => f.Key).ToListAsync();
            var newKeys = catalog.Values.Select(f => f.Key).ToHashSet();
            var removedKeys = oldKeys.Where(k => !newKeys.Contains(k)).ToList();

            if (removedKeys.Count > 0)
            {
                var workflowIds = await _context.WorkFlows.Where(x => x.RuleformconfigId == id)
                    .Select(x => x.Id).ToListAsync();

                if (workflowIds.Count > 0)
                {
                    var rulesInUse = await _context.RuleNodes
                        .Where(n => n.NodeType == RuleNodeType.Condition
                                    && n.FieldKey != null
                                    && removedKeys.Contains(n.FieldKey)
                                    && _context.Rules.Any(r => r.Id == n.RuleId && workflowIds.Contains(r.WorkflowId)))
                        .Select(n => n.RuleId)
                        .Distinct()
                        .CountAsync();

                    if (rulesInUse > 0)
                        throw new RuleValidationException(
                            $"Cannot remove field(s) {string.Join(", ", removedKeys)}: {rulesInUse} rule(s) still use them.");
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

           await _context.RuleFormConfigFields.Where(x => x.RuleformConfigId == id).ExecuteDeleteAsync();

            config.Update(input.Name.Trim(), input.Description?.Trim(), input.IsActive, input.AllowNestedGroups);



            var fields = BuildFields(input, catalog);
            foreach (var f in fields) f.UpdateRuleFormConfigId(id); // assign parent
            _context.RuleFormConfigFields.AddRange(fields);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return config;
        }

        public async Task DeleteAsync(Guid id)
        {
            var workflows = await _context.WorkFlows.CountAsync(x => x.RuleformconfigId == id);
            if (workflows > 0)
                throw new RuleValidationException(
                    $"Cannot delete: {workflows} workflow(s) use this form config. Unassign it first.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            await _context.RuleFormConfigFields.Where(x => x.RuleformConfigId == id).ExecuteDeleteAsync();
            var deleted = await _context.RuleFormConfigs.Where(x => x.Id == id).ExecuteDeleteAsync();
            if (deleted == 0) throw new KeyNotFoundException("Form config not found.");
            await tx.CommitAsync();
        }

        private static List<RuleFormConfigField> BuildFields(RuleFormConfig input, Dictionary<Guid, RuleField> catalog)
        {
            var list = new List<RuleFormConfigField>();
            var order = 0;
            foreach (var f in input.RuleFormConfigFields)
            {
                if (!catalog.TryGetValue(f.RulefieldId, out var field))
                    throw new RuleValidationException($"Unknown field: {f.RulefieldId}");
                 string? allowedJson = null;
                if (!string.IsNullOrEmpty(f.AllowedOperatorsJson))
                    allowedJson = JsonSerializer.Serialize(f.AllowedOperatorsJson);

                var cfgField = new RuleFormConfigField(Guid.Empty, f.RulefieldId, order++, f.IsMultiple,f.IsDefault, allowedJson);
                list.Add(cfgField);
            }

            return list;
        }

        private async Task<Dictionary<Guid, RuleField>> ValidateAsync(RuleFormConfig input, Guid? currentId)
        {
            var name = input.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new RuleValidationException("Form config name is required.");
            if (name.Length > 200)
                throw new RuleValidationException("Form config name is too long (max 200).");

            var lower = name.ToLower();
            if (await _context.RuleFormConfigs.AnyAsync(x => x.Id != currentId && x.Name.ToLower() == lower))
                throw new RuleValidationException($"A form config named '{name}' already exists.");

            var ids = input.RuleFormConfigFields.Select(f => f.RulefieldId).ToList();
            if (ids.Count != ids.Distinct().Count())
                throw new RuleValidationException("Duplicate fields are not allowed.");

            var catalog = await _context.RuleFields.Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id);
            if (catalog.Count != ids.Count)
                throw new RuleValidationException("Some selected fields do not exist.");

            return catalog;
        }

        
    }
}
