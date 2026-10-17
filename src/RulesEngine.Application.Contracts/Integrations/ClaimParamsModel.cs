using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Integrations
{
    public class ClaimParamsModel
    {
        public List<long>? workflows { get; set; }
        public List<ClaimValidationRequest>? claimValidationRequests { get; set; }
        public object? JsonPayload { get; set; }
    }
    public class ClaimValidationByCodePayload
    {
        public List<ClaimValidationByCodeRequest> ClaimValidationRequests { get; set; } = new();
    }
    public class ClaimValidationByCodeRequest
    {
        public string Patientnid { get; set; } = string.Empty;
        public List<string>? ActivityCodes { get; set; }
        public List<string>? DiagnosisCodes { get; set; }
        public List<MedicationQuantityData>? MedicationQuantityData { get; set; }
    }
    public class MedicationQuantityData
    {
        public string Code { get; set; }
        public decimal? Quantity { get; set; }
    }
    public class ClaimParamsByCodeModel
    {
        public List<long> workflows { get; set; }
        public object? JsonPayload { get; set; }
    }
    public class ClaimActivityRequestDto
    {
        public int ActivityId { get; set; }
        public string? SystemCode { get; set; }
        public decimal? Gross { get; set; }
        public decimal? NET { get; set; }
        public string? MediumDesc { get; set; }
        public decimal? Patient_share { get; set; }
        public decimal? MinUnitPrice { get; set; }
        public decimal? MaxUnitPrice { get; set; }
        public decimal? Quantity { get; set; }
        public List<string>? Tags { get; set; } = new();
        public string ActivityCode { get; set; }
    }

    public class ClaimValidationByCodeRequestExtended
    {
        public int ActivityId { get; set; }
        public string? Patientnid { get; set; }
        public int? PatientAge { get; set; }
        public List<string>? DiagnosisCodes { get; set; } = new();
        public List<string>? ActivityCodes { get; set; } = new();
        public decimal? ClaimedNet { get; set; }
        public decimal? EnteredNet { get; set; }
        public decimal? ClaimedGross { get; set; }
        public decimal? EnteredGross { get; set; }
        public DateTime? ClaimStartDate { get; set; }
        public DateTime? ClaimEndDate { get; set; }
        public string? BatchType { get; set; }
        public int? BatchServiceMonth { get; set; }
        public int? BatchServiceYear { get; set; }
        public List<string>? ClaimTags { get; set; } = new();
        public string? ClaimNumber { get; set; }
        public List<ClaimActivityRequestDto>? Activities { get; set; } = new();
    }
    public class ClaimValidationByCodePayloadExtended
    {
        public List<ClaimValidationByCodeRequestExtended> ClaimValidationRequests { get; set; } = new();
    }
}
