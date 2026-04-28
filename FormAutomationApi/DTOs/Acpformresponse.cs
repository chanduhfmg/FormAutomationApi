// DTOs/AcpFormResponse.cs

using System.Text.Json.Serialization;

namespace FormAutomationApi.DTOs
{
    public class AcpFormResponse
    {
        // ── Identity ──────────────────────────────────────────────────────────
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("patient_id")]
        public long? PatientId { get; set; }

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
        [JsonPropertyName("signature_first_name")]
        public string? SignatureFirstName { get; set; }

        [JsonPropertyName("signature_last_name")]
        public string? SignatureLastName { get; set; }

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
        public List<AcpAgentDto> Agents { get; set; } = new();

        [JsonPropertyName("witnesses")]
        public List<AcpWitnessResponseDto> Witnesses { get; set; } = new();
    }

    public class AcpWitnessResponseDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("date")] public string? Date { get; set; }
        [JsonPropertyName("address")] public string? Address { get; set; }
        [JsonPropertyName("signature_image")] public string? SignatureImage { get; set; }
    }

    public class AcpSubmitResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        // AUTO_INCREMENT id of the patient_acp_forms row
        [JsonPropertyName("form_id")]
        public long FormId { get; set; }

        // True = new row inserted; False = existing row updated
        [JsonPropertyName("is_new")]
        public bool IsNew { get; set; }

        // Resolved patient_id — always non-null.
        // For new patients: the AUTO_INCREMENT id just created in patients.
        // For existing patients: the supplied patient_id.
        [JsonPropertyName("patient_id")]
        public long PatientId { get; set; }

        [JsonPropertyName("form")]
        public AcpFormResponse? Form { get; set; }
    }
}