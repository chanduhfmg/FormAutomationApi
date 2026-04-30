namespace FormAutomationApi.DTOs
{
    public class ExpiredFormSent
    {
        public int PatientId { get; set; }
        public List<string> FormIds { get; set; } = new();
    }
}
