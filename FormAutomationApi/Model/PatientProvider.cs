public class PatientProvider
{
    public int PatientProviderId { get; set; }          // INT, AUTO_INCREMENT, PK

    public int PatientId { get; set; }                  // INT, FK to Patient, Not Null

    public string ProviderName { get; set; }            // VARCHAR(120), Not Null, No default

    public string? ProviderType { get; set; }           // VARCHAR(40), NULL

    public string? Notes { get; set; }                  // VARCHAR(400), NULL

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // DATETIME, CURRENT_TIMESTAMP

    // Navigation property
    public Patient Patient { get; set; }
}