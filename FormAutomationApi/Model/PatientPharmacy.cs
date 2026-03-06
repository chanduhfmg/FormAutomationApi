public class PatientPharmacy
{
    public int PatientPharmacyId { get; set; }          // INT, AUTO_INCREMENT, PK

    public int PatientId { get; set; }                  // INT, FK to Patient, Not Null

    public string PharmacyName { get; set; }            // VARCHAR(120), Not Null, No default

    public string? Location { get; set; }               // VARCHAR(120), NULL

    public string? Phone { get; set; }                  // VARCHAR(25), NULL

    public bool IsPreferred { get; set; } = true;       // TINYINT(1), Not Null, default '1'

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // DATETIME, CURRENT_TIMESTAMP

    // Navigation property
    public Patient Patient { get; set; }
}