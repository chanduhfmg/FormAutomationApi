namespace FormAutomationApi.DTOs
{
    public class SendForm
    {
        
        public string? PatientId { get; set; }
        public string PhoneNumber { get; set; }
        public string FormUrl { get; set; }

        public string? FacilityId { get; set; }

    }
}
