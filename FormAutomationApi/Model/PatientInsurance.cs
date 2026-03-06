using FormAutomationApi.Model;

public class PatientInsurance
{
    public int PatientInsuranceId { get; set; }         // INT, AUTO_INCREMENT, PK

    public int PatientId { get; set; }                  // INT, FK to Patient, Not Null

    public int? InsurancePlanId { get; set; }           // INT, FK to InsurancePlan, NULL

    public string CoverageType { get; set; }            // VARCHAR(20), Not Null, No default

    public string? MemberId { get; set; }               // VARCHAR(60), NULL

    public string? GroupNumber { get; set; }            // VARCHAR(60), NULL

    public string? SubscriberName { get; set; }         // VARCHAR(120), NULL (SubscriberNa...)

    public DateOnly? SubscriberDOB { get; set; }        // DATE, NULL

    public string? RelationshipType { get; set; }       // VARCHAR(40), NULL (RelationshipT...)

    public bool IsActive { get; set; } = true;          // TINYINT(1), default '1'

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // DATETIME, CURRENT_TIMESTAMP

    // Navigation properties
    public Patient Patient { get; set; }
    public InsurancePlan? InsurancePlan { get; set; }
}