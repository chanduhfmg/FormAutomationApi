using System.ComponentModel.DataAnnotations;

public class UnableToObtainSignature
{
    [Key]
    public int UnableId { get; set; }                   // INT, AUTO_INCREMENT, PK

    public int SignedDocumentId { get; set; }           // INT, FK to SignedDocument, Not Null

    public DateOnly? AttemptDate { get; set; }          // DATE, NULL

    public string? Reason { get; set; }                 // VARCHAR(300), NULL

    public string? StaffInitials { get; set; }          // VARCHAR(10), NULL

    // Navigation property
    public SignedDocument SignedDocument { get; set; }
}