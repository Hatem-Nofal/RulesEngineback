using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules.RuleBuilders
{
    [Table("RuleFormConfigFields", Schema = "RuleBuilders")]
    public sealed class RuleFormConfigField : FullAuditedEntity<long>
    {
        public long RuleformConfigId { get; private set; }

        [ForeignKey("RuleformConfigId")]

        public RuleFormConfig RuleformConfig { get; private set; }

        public long RulefieldId { get; private set; }

        [ForeignKey(nameof(RulefieldId))]

        public RuleField Rulefield { get; private set; }

        public string? AllowedOperatorsJson { get; private set; }
        public bool IsMultiple { get; private set; }

        public RuleFormConfigField()
        {
            
        }
        public RuleFormConfigField( long formConfigId, long ruleFieldId,  bool isMultiple,  string? allowedOperatorsJson = null)
        {
 
            RuleformConfigId = formConfigId;
            RulefieldId = ruleFieldId;
            AllowedOperatorsJson = allowedOperatorsJson;
            IsMultiple = isMultiple;
        }
        public void Update( long formConfigId, long ruleFieldId,  bool isMultiple, string? allowedOperatorsJson = null)
        {
             

            RuleformConfigId = formConfigId;
            RulefieldId = ruleFieldId;
            AllowedOperatorsJson = allowedOperatorsJson;
            IsMultiple = isMultiple;
        }

        public void Update( string? allowedOperatorsJson = null)
        {
            AllowedOperatorsJson = allowedOperatorsJson;
        }
        public void UpdateRuleFormConfigId(long ruleFormConfigId)
        {
            RuleformConfigId = ruleFormConfigId;
        }
    }
}
