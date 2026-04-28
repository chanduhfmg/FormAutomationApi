// Models/AcpAgent.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormAutomationApi.Model
{
    /// <summary>
    /// Maps to the acp_agents table.
    /// One PatientAcpForm has at most two rows: type = "primary" | "alternate".
    /// </summary>
    [Table("acp_agents")]
    public class AcpAgent
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("form_id")]
        public long FormId { get; set; }

        /// <summary>"primary" or "alternate"</summary>
        [Column("type")]
        [StringLength(20)]
        public string? Type { get; set; }

        [Column("name")]
        [StringLength(255)]
        public string? Name { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("city")]
        [StringLength(255)]
        public string? City { get; set; }

        [Column("phone")]
        [StringLength(50)]
        public string? Phone { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        [ForeignKey(nameof(FormId))]
        public PatientAcpForm? Form { get; set; }
    }
}