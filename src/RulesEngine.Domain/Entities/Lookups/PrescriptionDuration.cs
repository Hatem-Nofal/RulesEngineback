using RulesEngine.Lookups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Lookups
{
    [Table("PrescriptionDurations", Schema = "Lookups")]
    public class PrescriptionDuration : Entity<int>
    {
        [Required][StringLength(50)] public string DiagnosisGroup { get; set; }
        [Required] public int DurationDays { get; set; }
        [JsonIgnore] public List<Diagnosis> Diagnoses { get; set; } = new();


        protected PrescriptionDuration() { }
        public PrescriptionDuration(int id, string diagnosisGroup, int durationDays)
        {
            Id = id;

            DiagnosisGroup = diagnosisGroup;

            DurationDays = durationDays;

        }
    }
}
