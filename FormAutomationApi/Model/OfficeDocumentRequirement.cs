public class OfficeDocumentRequirement
{
    public int OfficeId { get; set; }           // INT, No default, FK to Office

    public int DocumentTypeId { get; set; }     // INT, No default (DocumentTy... column)

    public bool IsRequired { get; set; } = false;  // TINYINT(1), default '0'

    public bool IsActive { get; set; } = true;     // TINYINT(1), default '1'

    // Navigation properties
    public Office Office { get; set; }
}