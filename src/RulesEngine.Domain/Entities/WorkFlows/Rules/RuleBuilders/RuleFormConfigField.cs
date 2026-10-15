using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleFormConfigFields", Schema = "RuleBuilders")]
    public sealed class RuleFormConfigField : FullAuditedEntity<Guid>
    {
        public Guid RuleformConfigId { get; private set; }

        [ForeignKey("RuleformConfigId")]

        public RuleFormConfig RuleformConfig { get; private set; }

        public Guid RulefieldId { get; private set; }

        [ForeignKey(nameof(RulefieldId))]

        public RuleField Rulefield { get; private set; }

        public int Order { get; private set; }
        public string? AllowedOperatorsJson { get; private set; }
        public bool IsMultiple { get; private set; }

        public RuleFormConfigField()
        {
            
        }
        public RuleFormConfigField( Guid formConfigId, Guid ruleFieldId, int order, bool isMultiple, string? allowedOperatorsJson = null)
        {
            Id = Guid.CreateVersion7();

            RuleformConfigId = formConfigId;
            RulefieldId = ruleFieldId;
            Order = order;
            AllowedOperatorsJson = allowedOperatorsJson;
            IsMultiple = isMultiple;
        }

        public void Update(int order, string? allowedOperatorsJson = null)
        {
            Order = order;
            AllowedOperatorsJson = allowedOperatorsJson;
        }
        public void UpdateRuleFormConfigId(Guid ruleFormConfigId)
        {
            RuleformConfigId = ruleFormConfigId;
        }
    }
}
