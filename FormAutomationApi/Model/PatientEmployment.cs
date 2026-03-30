using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PatientEmployment
{
    [Key]
    public int PatientEmploymentId { get; set; }      // INT, AUTO_INCREMENT, PK
   
    [ForeignKey("PatientId")]
    public int PatientId { get; set; }                // INT, FK to Patient

    public string? EmployerName { get; set; }         // VARCHAR(120), NULL

    public string? Occupation { get; set; }           // VARCHAR(80), NULL

    public string? EmployerAddress { get; set; }      // VARCHAR(200), NULL (EmployerAddr...)

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;  // DATETIME, CURRENT_TIMESTAMP

    // Navigation property
    public Patient? Patient { get; set; }
}