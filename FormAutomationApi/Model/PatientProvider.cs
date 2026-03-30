public class PatientProvider
{
    public int PatientProviderId { get; set; }
    public int PatientId { get; set; }
    public string ProviderName { get; set; }
    public string? ProviderType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

                     // ← add this
    public Patient? Patient { get; set; }
}