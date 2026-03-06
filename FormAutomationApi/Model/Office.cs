public class Office
{
    public int OfficeId { get; set; }                    // INT, AUTO_INCREMENT, PK

    public string OfficeName { get; set; }               // VARCHAR(120), NOT NULL

    public string? Phone { get; set; }                   // VARCHAR(25), NULL

    public string? AddressLine1 { get; set; }            // VARCHAR(120), NULL

    public string? AddressLine2 { get; set; }            // VARCHAR(120), NULL

    public string? City { get; set; }                    // VARCHAR(80), NULL

    public string? State { get; set; }                   // VARCHAR(20), NULL

    public string? ZipCode { get; set; }                 // VARCHAR(15), NULL

    public bool IsActive { get; set; } = true;           // TINYINT(1), default '1'
}