public class MailRequest
{
    public string From { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }

    // Add these:
    public string FormName { get; set; }
    public string FacilityName { get; set; }
    public string CategoryName { get; set; }
    public string Notes { get; set; }
    public string SubmittedBy { get; set; }

    public IFormFile File { get; set; } // 🔥 IMPORTANT
}