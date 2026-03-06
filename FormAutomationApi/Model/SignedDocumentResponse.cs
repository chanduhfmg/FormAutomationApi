public class SignedDocumentResponse
{
    public int ResponseId { get; set; }                 // INT, AUTO_INCREMENT, PK

    public int SignedDocumentId { get; set; }           // INT, FK to SignedDocument, Not Null

    public string QuestionCode { get; set; }            // VARCHAR(80), Not Null, No default

    public string ResponseType { get; set; }            // VARCHAR(20), Not Null, No default

    public bool? BoolValue { get; set; }                // TINYINT(1), NULL

    public string? TextValue { get; set; }              // VARCHAR(500), NULL

    public DateOnly? DateValue { get; set; }            // DATE, NULL

    public string? ChoiceValue { get; set; }            // VARCHAR(120), NULL

    // Navigation property
    public SignedDocument SignedDocument { get; set; }
}