using RulesEngine.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{
    [Table("Diagnoses", Schema = "Lookups")]
    public class Diagnosis : Entity<long>
    {
        public string iqvia_code { get; set; }
        public string Short_Desc { get; set; }
        public string Medium_Desc { get; set; }
        public int Status { get; set; }

        public string iqvia_type { get; set; }


        public DateTime Create_timestamp { get; set; }

        public DateTime Update_timestamp { get; set; }

   
        public bool IsUncovered { get; set; } = false; 
        public DiagnosisFlag? DiagnosisFlag { get; set; }
        public int? PrescriptionDurationId { get; set; }
        [ForeignKey(nameof(PrescriptionDurationId))] public PrescriptionDuration? PrescriptionDuration { get; set; }
        public bool IsActive { get; set; } = false; public List<DiagnosisMorbidityGroup> DiagnosisMorbidityGroups { get; set; } = new();





        private Diagnosis() { }

        public Diagnosis(long id, string iqviaCode, string shortDesc, int status)
        {
            Id = id;
            iqviaCode = iqviaCode;
            Short_Desc = shortDesc;
            Status = status;
            Create_timestamp = DateTime.UtcNow;
            Update_timestamp = Create_timestamp;
        }

        public void UpdateDescription(string shortDesc, string mediumDesc)
        {
            Short_Desc = shortDesc;
            Medium_Desc = mediumDesc;
            Update_timestamp = DateTime.UtcNow;
        }
    }
}
