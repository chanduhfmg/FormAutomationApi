using System.ComponentModel.DataAnnotations;

namespace FormAutomationApi.Model
{
    public class InsurancePlan
    {
        [Key]
        public int InsurancePlanId {  get; set; }

        public string PlanName {  get; set; }

        public string PayerName {  get; set; }

        public string? Notes {  get; set; }
    }
}
