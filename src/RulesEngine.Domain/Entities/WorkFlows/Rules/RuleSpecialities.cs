using RulesEngine.Entities.Lookups;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    [Table("RuleSpecialities")]
    public sealed class RuleSpecialities : Entity<long>
    {
        public long RuleId { get; private set; }
        [ForeignKey("RuleId")]

        public Rule Rule { get; private set; }
        public int SpecialitiesId { get; private set; }
        [ForeignKey("SpecialitiesId")]

        public Specialities Specialitiy { get; private set; }

 
        public RuleSpecialities(long id, long ruleId, int specialitiesId)
        {
            Id = id;
            RuleId = ruleId;
            SpecialitiesId = specialitiesId;
        }
    }
}
