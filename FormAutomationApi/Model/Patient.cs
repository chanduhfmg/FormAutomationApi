using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Patient
{
    [Key]
    public int PatientId { get; set; }

    [Required]
    [StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(1)]
    public string? MiddleInitial { get; set; }

    [StringLength(120)]
    public string? AddressLine1 { get; set; }

    [StringLength(120)]
    public string? AddressLine2 { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? State { get; set; }

    [StringLength(15)]
    public string? ZipCode { get; set; }

    [StringLength(254)]
    public string? Email { get; set; }

    [StringLength(25)]
    public string? PhonePrimary { get; set; }

    [StringLength(25)]
    public string? PhoneAlternate { get; set; }

    [StringLength(20)]
    public string? Sex { get; set; }

    [StringLength(30)]
    public string? MaritalStatus { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public byte[]? SSN_Encrypted { get; set; }

    [StringLength(4)]
    public string? SSN_Last4 { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [StringLength(4)]
    public string? Initials { get; set; }

    public string? Apt { get; set; }


    
}