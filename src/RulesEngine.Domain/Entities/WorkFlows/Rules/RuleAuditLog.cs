using RulesEngine.Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    [Table("RuleAuditLogs")]
    public sealed class RuleAuditLog : FullAuditedEntity<long>
    {
    
        public long RuleId { get; private set; }

        [ForeignKey(nameof(RuleId))]

        public Rule Rule { get; private set; }
        public ViolationType ViolationType { get; private set; }
        public string? ViolationMessage { get; private set; }
        public bool IsActive { get; private set; }
        public string Expression { get; private set; }



        public static RuleAuditLog Create(long ruleId, Rule rule, ViolationType violationType, string? violationMessage, bool isActive, string expression)
        {
            return new RuleAuditLog()
            {
                RuleId = ruleId,
                Rule = rule,
                ViolationType = violationType,
                ViolationMessage = violationMessage,
                IsActive = isActive,
                Expression = expression
            };
         
        }

    }
}
