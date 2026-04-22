using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormAutomationApi.Model
{
    public class DocumentVersionOffice
    {
        [Key]
        public int Id { get; set; }

        public int DocumentVersionId { get; set; }

        public int OfficeId { get; set; }

        // Optional navigation properties
        public DocumentVersion DocumentVersion { get; set; }
        public Office Office { get; set; }
    }
}