using System.ComponentModel.DataAnnotations.Schema;

namespace FormAutomationApi.Model
{
    [Table("intakepacket")]
    public class IntakePacket
    {
        public int IntakePacketId { get; set; }

        public int PatientId { get; set; }
        public DateTime PacketDate {get;set;}

        public string LocationName {  get; set; }

        public DateTime CreatedAt { get; set; }

        public int OfficeId {  get; set; }


    }
}
