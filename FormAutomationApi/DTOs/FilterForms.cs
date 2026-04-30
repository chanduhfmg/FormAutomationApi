using Org.BouncyCastle.Bcpg.OpenPgp;

namespace FormAutomationApi.DTOs
{
    public class FilterForms
    {
        public int PatientId { get; set; }
        public string FacilityId { get; set; }

        public string MobileNumber { get; set; }
    }
}
