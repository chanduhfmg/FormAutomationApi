using FormAutomationApi.Model;

public class SignedDocument
{
    public int SignedDocumentId { get; set; }           // INT, AUTO_INCREMENT, PK

    public int IntakePacketId { get; set; }             // INT, FK to IntakePacket, Not Null

    public int DocumentTypeId { get; set; }             // INT, FK to DocumentType, Not Null

    public string? SignedByName { get; set; }           // VARCHAR(120), NULL

    public string? SignedByRole { get; set; }           // VARCHAR(40), NULL

    public string? RepresentativeAuthority { get; set; }         // VARCHAR(200), NULL (Representativ...)

    public DateTime? SignedAt { get; set; }             // DATETIME, NULL

    public bool SignatureCaptured { get; set; } = false; // TINYINT(1), Not Null, default '0'

    public string? Notes { get; set; }                  // VARCHAR(400), NULL

    public int? DocumentVersionId { get; set; }         // INT, NULL (DocumentVer...)

    // Navigation properties
    public IntakePacket? IntakePacket { get; set; }
    public DocumentType? DocumentType { get; set; }
}