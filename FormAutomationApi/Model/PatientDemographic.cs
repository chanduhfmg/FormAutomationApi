public class PatientDemographic
{
    public int PatientId { get; set; }                // INT, FK to Patient

    public string? Language { get; set; }             // VARCHAR(60), NULL

    public string? Race { get; set; }                 // VARCHAR(60), NULL

    public string? Ethnicity { get; set; }            // VARCHAR(60), NULL

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;  // DATETIME, CURRENT_TIMESTAMP

    // Navigation property
    public Patient Patient { get; set; }
}