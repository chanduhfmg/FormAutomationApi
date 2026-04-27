// Models/PatientAcpForm.cs
//
// ── Run this SQL before starting the app ─────────────────────────────────────
//
//   -- If upgrading from old schema that had signature_name:
//   ALTER TABLE patient_acp_forms
//     DROP COLUMN  IF EXISTS signature_name,
//     ADD COLUMN   signature_first_name VARCHAR(120) NULL AFTER signature_date,
//     ADD COLUMN   signature_last_name  VARCHAR(120) NULL AFTER signature_first_name,
//     ADD COLUMN   signature_phone      VARCHAR(30)  NULL AFTER signature_last_name,
//     ADD COLUMN   signature_city       VARCHAR(100) NULL AFTER signature_address,
//     ADD COLUMN   signature_state      VARCHAR(50)  NULL AFTER signature_city,
//     ADD COLUMN   signature_zip        VARCHAR(20)  NULL AFTER signature_state;
//
//   -- If creating fresh:
//   (include all columns below in your CREATE TABLE statement)
//
// ─────────────────────────────────────────────────────────────────────────────

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormAutomationApi.Model
{
    [Table("patient_acp_forms")]
    public class PatientAcpForm
    {
        // ── Primary key ───────────────────────────────────────────────────────
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // ── Foreign key → patients.PatientId ─────────────────────────────────
        // Set by ResolvePatientAsync — NEVER null after controller runs.
        [Column("patient_id")]
        public long? PatientId { get; set; }

        // ── Part I ────────────────────────────────────────────────────────────
        [Column("patient_name")]
        public string? PatientName { get; set; }

        [Column("agent_limits")]
        public string? AgentLimits { get; set; }

        [Column("proxy_expiry")]
        public string? ProxyExpiry { get; set; }

        [Column("agent_instructions")]
        public string? AgentInstructions { get; set; }

        // ── Part II ───────────────────────────────────────────────────────────
        [Column("lw_name")]
        public string? LwName { get; set; }

        [Column("life_choice")]
        public string? LifeChoice { get; set; }

        [Column("no_cpr")]
        public bool NoCpr { get; set; }

        [Column("no_vent")]
        public bool NoVent { get; set; }

        [Column("no_nutrition")]
        public bool NoNutrition { get; set; }

        [Column("no_antibiotics")]
        public bool NoAntibiotics { get; set; }

        [Column("pain_limit")]
        public string? PainLimit { get; set; }

        [Column("other_directions")]
        public string? OtherDirections { get; set; }

        // ── Organ donation ────────────────────────────────────────────────────
        [Column("organ_choice")]
        public string? OrganChoice { get; set; }

        [Column("organ_spec")]
        public string? OrganSpec { get; set; }

        [Column("purpose_transplant")]
        public bool PurposeTransplant { get; set; }

        [Column("purpose_therapy")]
        public bool PurposeTherapy { get; set; }

        [Column("purpose_research")]
        public bool PurposeResearch { get; set; }

        [Column("purpose_education")]
        public bool PurposeEducation { get; set; }

        // ── Part III — signature block ─────────────────────────────────────────
        // First name and last name stored separately — no combined name column.
        // These also populate patients.FirstName / patients.LastName for new patients.

        [Column("signature_first_name")]
        public string? SignatureFirstName { get; set; }   // → patients.FirstName

        [Column("signature_last_name")]
        public string? SignatureLastName { get; set; }    // → patients.LastName

        [Column("signature_date")]
        public DateTime? SignatureDate { get; set; }

        [Column("signature_phone")]
        public string? SignaturePhone { get; set; }       // → patients.PhonePrimary

        [Column("signature_address")]
        public string? SignatureAddress { get; set; }     // → patients.AddressLine1

        [Column("signature_city")]
        public string? SignatureCity { get; set; }        // → patients.City

        [Column("signature_state")]
        public string? SignatureState { get; set; }       // → patients.State

        [Column("signature_zip")]
        public string? SignatureZip { get; set; }         // → patients.ZipCode

        [Column("signature_image", TypeName = "LONGTEXT")]
        public string? SignatureImage { get; set; }

        // ── Timestamps ────────────────────────────────────────────────────────
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}