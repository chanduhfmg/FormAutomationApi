using FormAutomationApi.Model;

namespace FormAutomationApi.DTOs
{
    public class SendFormRequest
    {
        public string Group { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string FacilityId { get; set; } = string.Empty;
    }

    public class RequestFormSubmission
    {
        public Patient Patient { get; set; }
        public PatientDemographic? PatientDemographic { get; set; }
        public PatientEmployment? PatientEmployment { get; set; }
        public PatientPharmacy? PatientPharmacy { get; set; }
        public PatientInsurance? PatientInsurance { get; set; }
        public PatientOffice? PatientOffice { get; set; }
        public EmergencyContact? EmergencyContact { get; set; }
        public PatientProvider? PatientProvider { get; set; }
        public IntakePacket? IntakePacket { get; set; }
        public SignedDocument? SignedDocument { get; set; }
        public UnableToObtainSignature? UnableToObtainSignature { get; set; }  // ← was required
        public List<HipaaFamilyMember>? HipaaFamilyMembers { get; set; }
        public List<SignedDocumentResponse>? SignedDocumentResponses { get; set; }
        //public FormSubmission? FormSubmission { get; set; }
    }
}
