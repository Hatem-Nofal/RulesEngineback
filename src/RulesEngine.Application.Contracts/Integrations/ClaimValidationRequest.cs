using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Integrations
{
    public class ClaimValidationRequest
    {
          public string PatientId { get; set; } = string.Empty;
        public int? PatientAge { get; set; }
        public string Patientnid { get; set; }
        public string? PatientGender { get; set; } = string.Empty;
        public List<string>? Diagnoses { get; set; } = new();
        public List<string>? CurrentMedications { get; set; } = new();
        public List<string>? Procedures { get; set; } = new();
        public List<string>? Consumables { get; set; } = new();
        public Dictionary<string, int> DrugQuantityByCode { get; set; } = new();
        public List<long>? DiagnosisIds { get; set; }
        public List<long>? ActivityTypeIds { get; set; }
        public Dictionary<string, int>? DiagnosisCounts { get; set; }
        public Dictionary<string, int>? MedicationCounts { get; set; }
        public Dictionary<string, decimal>? MedicationQuantities { get; set; }
        public Dictionary<long, decimal>? ActivityQuantitiesByTypeId { get; set; }
    }
}
