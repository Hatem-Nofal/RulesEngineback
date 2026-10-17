using RulesEngine.Integrations;
using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Integrations
{
    public class ClaimDto
    {

        public Guid Id { get; set; }
        public string PatientId { get; set; }
        public List<DiagnosisDto> Diagnoses { get; set; }
        public List<MedicationDto> CurrentMedications { get; set; }
        public List<ProcedureDto> Procedures { get; set; }
        public List<ConsumableDto> Consumables { get; set; }
        public List<DrugQuantityDto> DrugQuantities { get; set; }
    }
    public class ClaimValidation
    {
        public List<Guid>? workflows { get; set; }
        public List<string>? workflownames { get; set; }
        public List<ClaimValidationByCodeRequestExtended> ClaimValidationRequests { get; set; } = new();
    }
}
