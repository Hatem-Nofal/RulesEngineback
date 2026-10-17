using RulesEngine.Common.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleFormConfigs", Schema = "RuleBuilders")]
    public sealed class RuleFormConfig : FullAuditedEntity<Guid>
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public bool IsActive { get; private set; } = true;
        public bool AllowNestedGroups { get; private set; } = true;
        public string? StrategyKey { get; private set; }

        // EF mapping property - kept as list with private setter
        public List<RuleFormConfigField> RuleFormConfigFields { get; private set; } = new();

        [NotMapped]
        public IReadOnlyCollection<RuleFormConfigField> _RuleFormConfigFields => RuleFormConfigFields.AsReadOnly();

 
        public RuleFormConfig(  string name, string? description = null)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            StrategyKey = name.NormalizeSpace();
        }

        public void AddField(RuleFormConfigField field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            RuleFormConfigFields.Add(field);
        }
        public void AddFields(IEnumerable<RuleFormConfigField> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            RuleFormConfigFields.AddRange(fields);
        }

        public void RemoveField(RuleFormConfigField field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            RuleFormConfigFields.Remove(field);
        }

        public void ResetRuleFormConfigFields( )
        {
             RuleFormConfigFields=new();
        }
        public void Update(string name, string? description = null, bool isActive = true, bool allowNestedGroups = true)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            IsActive = isActive;
            AllowNestedGroups = allowNestedGroups;
            StrategyKey = name.NormalizeSpace();
        }
    }
}
