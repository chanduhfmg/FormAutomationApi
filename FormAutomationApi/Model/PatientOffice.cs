public class PatientOffice
{
    public int PatientOfficeId { get; set; }            // INT, AUTO_INCREMENT, PK

    public int PatientId { get; set; }                  // INT, FK to Patient, Not Null

    public int OfficeId { get; set; }                   // INT, FK to Office, Not Null

    public bool? IsPrimary { get; set; } = false;       // TINYINT(1), NULL, default '0'

    public DateOnly? FirstVisitDate { get; set; }       // DATE, NULL

    public bool? Active { get; set; } = true;           // TINYINT(1), NULL, default '1'

    // Navigation properties
    public Patient Patient { get; set; }
    public Office Office { get; set; }
}