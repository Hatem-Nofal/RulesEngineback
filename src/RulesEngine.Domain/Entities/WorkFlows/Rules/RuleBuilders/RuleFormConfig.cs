using RulesEngine.Common.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleFormConfigs", Schema = "RuleBuilders")]
    public sealed class RuleFormConfig : FullAuditedEntity<long>
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public bool AllowNestedGroups { get; private set; } = false;
        public string? StrategyKey { get; private set; }

  

        private readonly List<WorkFlow> _workFlows = new();

        public IReadOnlyCollection<WorkFlow> WorkFlows => _workFlows.AsReadOnly();
        private readonly List<RuleFormConfigField> _ruleFormConfigFields = new();

        public IReadOnlyCollection<RuleFormConfigField> RuleFormConfigFields => _ruleFormConfigFields.AsReadOnly();

        public RuleFormConfig(  string name, bool allowNestedGroups, string? description = null)
        {
             Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            StrategyKey = name.NormalizeSpace();
            AllowNestedGroups = allowNestedGroups;
        }

        public void AddField(RuleFormConfigField field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            _ruleFormConfigFields.Add(field);
        }
        public void AddRuleFormConfigFields(IEnumerable<RuleFormConfigField> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            _ruleFormConfigFields.AddRange(fields);
        }

        public void RemoveField(RuleFormConfigField field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            _ruleFormConfigFields.Remove(field);
        }

        public void ResetRuleFormConfigFields( )
        {
            _ruleFormConfigFields.Clear();
        }
        public void Update(string name, string? description , bool allowNestedGroups, bool isActive = true )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            IsActive = isActive;
            AllowNestedGroups = allowNestedGroups;
            StrategyKey = name.NormalizeSpace();
        }
    }
}
