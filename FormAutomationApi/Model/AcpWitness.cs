// Models/AcpWitness.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormAutomationApi.Model
{
    /// <summary>
    /// Maps to the acp_witnesses table.
    /// One PatientAcpForm has at most two witness rows (ordered by id ASC).
    /// </summary>
    [Table("acp_witnesses")]
    public class AcpWitness
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("form_id")]
        public long FormId { get; set; }

        [Column("name")]
        [StringLength(255)]
        public string? Name { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("date")]
        public DateTime? Date { get; set; }

        /// <summary>Base-64 PNG of the witness's drawn signature.</summary>
        [Column("signature_image")]
        public string? SignatureImage { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        [ForeignKey(nameof(FormId))]
        public PatientAcpForm? Form { get; set; }
    }
}