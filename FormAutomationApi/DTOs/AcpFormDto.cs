// DTOs/AcpFormRequest.cs

using System.Text.Json.Serialization;

namespace FormAutomationApi.DTOs
{
    // ── Sent only when patient_id is null (new patient) ───────────────────────
    // Values come directly from Part III of the ACP form.
    // Map 1-to-1 to columns in the `patients` table.
    public class AcpPatientInfo
    {
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }        // → patients.FirstName   (NOT NULL)

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }         // → patients.LastName    (NOT NULL)

        [JsonPropertyName("phone_primary")]
        public string? PhonePrimary { get; set; }     // → patients.PhonePrimary

        [JsonPropertyName("address_line1")]
        public string? AddressLine1 { get; set; }     // → patients.AddressLine1

        [JsonPropertyName("city")]
        public string? City { get; set; }             // → patients.City

        [JsonPropertyName("state")]
        public string? State { get; set; }            // → patients.State

        [JsonPropertyName("zip_code")]
        public string? ZipCode { get; set; }          // → patients.ZipCode
    }

    public class AcpFormRequest
    {
        // ── Identity ──────────────────────────────────────────────────────────
        // Null  → new patient: backend creates patients row first using PatientInfo.
        // Set   → existing patient: backend finds and updates existing ACP form.
        [JsonPropertyName("patient_id")]
        public long? PatientId { get; set; }

        // ── New-patient info (ignored when PatientId is non-null) ─────────────
        [JsonPropertyName("patient_info")]
        public AcpPatientInfo? PatientInfo { get; set; }

        // ── Part I ────────────────────────────────────────────────────────────
        [JsonPropertyName("patient_name")]
        public string? PatientName { get; set; }

        [JsonPropertyName("agent_limits")]
        public string? AgentLimits { get; set; }

        [JsonPropertyName("proxy_expiry")]
        public string? ProxyExpiry { get; set; }

        [JsonPropertyName("agent_instructions")]
        public string? AgentInstructions { get; set; }

        // ── Part II ───────────────────────────────────────────────────────────
        [JsonPropertyName("lw_name")]
        public string? LwName { get; set; }

        [JsonPropertyName("life_choice")]
        public string? LifeChoice { get; set; }

        [JsonPropertyName("no_cpr")]
        public bool NoCpr { get; set; }

        [JsonPropertyName("no_vent")]
        public bool NoVent { get; set; }

        [JsonPropertyName("no_nutrition")]
        public bool NoNutrition { get; set; }

        [JsonPropertyName("no_antibiotics")]
        public bool NoAntibiotics { get; set; }

        [JsonPropertyName("pain_limit")]
        public string? PainLimit { get; set; }

        [JsonPropertyName("other_directions")]
        public string? OtherDirections { get; set; }

        // ── Organ donation ────────────────────────────────────────────────────
        [JsonPropertyName("organ_choice")]
        public string? OrganChoice { get; set; }

        [JsonPropertyName("organ_spec")]
        public string? OrganSpec { get; set; }

        [JsonPropertyName("purpose_transplant")]
        public bool PurposeTransplant { get; set; }

        [JsonPropertyName("purpose_therapy")]
        public bool PurposeTherapy { get; set; }

        [JsonPropertyName("purpose_research")]
        public bool PurposeResearch { get; set; }

        [JsonPropertyName("purpose_education")]
        public bool PurposeEducation { get; set; }

        // ── Part III — signature block ─────────────────────────────────────────
        // First name and last name are now separate fields — no more splitting.
        // Both are stored in patient_acp_forms AND used to create the patient row.

        [JsonPropertyName("signature_first_name")]
        public string? SignatureFirstName { get; set; }   // → patients.FirstName

        [JsonPropertyName("signature_last_name")]
        public string? SignatureLastName { get; set; }    // → patients.LastName

        [JsonPropertyName("signature_date")]
        public string? SignatureDate { get; set; }

        [JsonPropertyName("signature_phone")]
        public string? SignaturePhone { get; set; }

        [JsonPropertyName("signature_address")]
        public string? SignatureAddress { get; set; }

        [JsonPropertyName("signature_city")]
        public string? SignatureCity { get; set; }

        [JsonPropertyName("signature_state")]
        public string? SignatureState { get; set; }

        [JsonPropertyName("signature_zip")]
        public string? SignatureZip { get; set; }

        [JsonPropertyName("signature_image")]
        public string? SignatureImage { get; set; }

        // ── Relations ─────────────────────────────────────────────────────────
        [JsonPropertyName("agents")]
        public List<AcpAgentDto>? Agents { get; set; }

        [JsonPropertyName("witnesses")]
        public List<AcpWitnessDto>? Witnesses { get; set; }
    }

    public class AcpAgentDto
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("address")] public string? Address { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }

    public class AcpWitnessDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("date")] public string? Date { get; set; }
        [JsonPropertyName("address")] public string? Address { get; set; }
        [JsonPropertyName("signature_image")] public string? SignatureImage { get; set; }
    }
}