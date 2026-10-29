using RulesEngine.Entities.Lookups;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    [Table("RuleDiagnosis")]
    public sealed class RuleDiagnosis : Entity<long>
    {
        public Guid RuleId { get; private set; }
        [ForeignKey("RuleId")]

        public Rule Rule { get; private set; }

        public long DiagnosisId { get; private set; }
        [ForeignKey("DiagnosisId")]
        public Diagnosis Diagnosis { get; private set; }
        public bool? IsSecondary { get; private set; }

 
        public RuleDiagnosis(long id, Guid ruleId, long diagnosisId, bool? isSecondary = null)
        {
            Id = id;
            RuleId = ruleId;
            DiagnosisId = diagnosisId;
            IsSecondary = isSecondary;
        }
    }
}
