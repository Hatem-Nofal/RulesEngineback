using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{
    [Table("DiagnosisMorbidityGroups", Schema = "Lookups")]

    public class DiagnosisMorbidityGroup : Entity<long>
    {
        public long DiagnosisId { get; set; }
        [ForeignKey(nameof(DiagnosisId))] 
        public Diagnosis Diagnosis { get; set; }
        public int MorbidityGroupId { get; set; }
        [ForeignKey(nameof(MorbidityGroupId))] 
        public MorbidityGroup MorbidityGroup { get; set; }
        public override object[] GetKeys() => new object[] { DiagnosisId, MorbidityGroupId };

    }
}
