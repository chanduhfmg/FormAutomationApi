using System.ComponentModel.DataAnnotations;

namespace FormAutomationApi.Model
{
    public class FormSubmission
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        // Keep PatientId (no navigation → no FK issues)
        public int? PatientId { get; set; }

        public int SenderId { get; set; }

        // Store multiple form IDs (simple approach)
        public string FormIds { get; set; } = string.Empty;

        // Better than string
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? OfficeId { get; set; }

    }

    public enum SubmissionStatus
    {
        Pending,
        Completed,
        Expired
    }
}
