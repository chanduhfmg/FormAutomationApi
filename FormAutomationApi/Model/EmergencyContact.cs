namespace FormAutomationApi.Model
{
    public class EmergencyContact
    {
        public int EmergencyContactId {  get; set; }
        public int PatientId {  get; set; }

        public string ContactName {  get; set; }

        public string Relationship { get; set;  }
         public string Phone {  get; set; }
        public int IsPrimary { get; set; }

        public DateTime CreatedAt {  get; set; }
        
    }
}
