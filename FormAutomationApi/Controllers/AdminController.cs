using FormAutomationApi.Context;
using FormAutomationApi.DTOs;
using FormAutomationApi.Model;
using FormAutomationApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Mail;
using Microsoft.Exchange.WebServices.Data;

namespace FormAutomationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly TwilioService _twilioService;
        private readonly IConfiguration _config;

        public AdminController(ApplicationDbContext context, ITokenService tokenService, TwilioService twilioService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _twilioService = twilioService;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
            var allfacilities = await _context.Offices.ToListAsync();

            return Ok(allfacilities);

            }catch (Exception ex) {
                return StatusCode(500, new { message = "Error sending SMS", error = ex.Message });

            }

        }

        [HttpGet("facilities-with-forms")]
        public async Task<IActionResult> GetFacilitiesWithForms(int page = 1, int pageSize = 10)
        {
            var query = _context.Offices;

            var totalCount = await query.CountAsync();

            var facilities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OfficeId,
                    o.OfficeName,
                    Forms = _context.DocumentVersionOffices
                        .Where(dvo => dvo.OfficeId == o.OfficeId)
                        .Join(_context.DocumentVersions,
                            dvo => dvo.DocumentVersionId,
                            dv => dv.DocumentVersionId,
                            (dvo, dv) => new
                            {
                                dv.DocumentVersionId,
                                dv.VersionLabel,
                                dv.TemplatePath
                            })
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                data = facilities,
                totalCount,
                page,
                pageSize
            });
        }



        //adding the twilio service
        [HttpPost("twilio-send")]
        public async Task<IActionResult> SendSms([FromBody] SendForm request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.FormUrl))
            {
                return BadRequest("Phone or FormLink missing");
            }

            try
            {
                var submission = new FormSubmission
                {
                    CreatedAt = DateTime.Now,
                    PatientId = int.TryParse(request.PatientId, out var pid) ? pid : null,
                    ExpiresAt = DateTime.Now.AddDays(7),
                    FormIds = request.FormLink,
                    SenderId = 0 , 
                    OfficeId = request.officeId

                };

                await _context.FormSubmissions.AddAsync(submission);

                // ⚡ This actually saves it to the DB
                await _context.SaveChangesAsync();

                //create form link in this
                var baseUrl = _config["Frontend:FormsUrl"];
                string formLink = $"{baseUrl}/subforms?token={submission.SessionId}";


                var sent = await _twilioService.SendFormLink(request.PhoneNumber, formLink);
                return Ok(new { message = "SMS sent successfully", sent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending SMS", error = ex.Message });

            }
        }

        // filter submissin through facility id
        [HttpGet("get-sessions/filter")]
        public async Task<IActionResult> FilterSession([FromQuery] string? officeIds)
        {
            try
            {
                IQueryable<FormSubmission> query = _context.FormSubmissions;

                // Apply filter ONLY if provided
                if (!string.IsNullOrWhiteSpace(officeIds))
                {
                    var officeIdList = officeIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();

                    query = query.Where(s => s.OfficeId != null &&
                                             officeIdList.Any(id =>
                                                 ("," + s.OfficeId + ",").Contains("," + id + ",")
                                             ));
                }

                // Always limit results (important for performance)
                var sessions = await query
                    .OrderByDescending(s => s.CreatedAt) // latest first
                    .Take(100)
                    .ToListAsync();

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error filtering sessions", error = ex.Message });
            }
        }

        [HttpGet("get-sessions")]
        public async Task<IActionResult> GetSessions()
        {
            // Logic to retrieve sessions from the database
            try
            {


                var sessions = await _context.FormSubmissions.ToListAsync();

                var expiredSessions = sessions.Where(s => s.ExpiresAt < DateTime.Now && s.Status != SubmissionStatus.Completed).ToList();
                if (expiredSessions.Any())
                {
                    expiredSessions.ForEach(s => s.Status = SubmissionStatus.Expired);
                    await _context.SaveChangesAsync();
                }
                return Ok(sessions);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving sessions", error = ex.Message });
            }
        }

        [HttpGet("get-session/{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            try
            {
                var sessionGuid = Guid.Parse(sessionId);
                // Logic to retrieve a specific session from the database
                var session = await _context.FormSubmissions.FirstOrDefaultAsync(s => s.SessionId == sessionGuid);
                if (session == null)
                {
                    return NotFound(new { message = "Session not found" });
                }
                return Ok(session);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Invalid session ID format" });

            }
        }

        [HttpPost("send-mail")]
        public async Task<IActionResult> SendMail([FromForm] MailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.From))
                    return BadRequest("From is required");

                var toList = _config.GetSection("MailSettings:To").Get<string[]>();
                var ccList = _config.GetSection("MailSettings:Cc").Get<string[]>();

                if ((toList == null || !toList.Any()) && (ccList == null || !ccList.Any()))
                    return BadRequest("No recipients configured");

                

                var service = new ExchangeService(ExchangeVersion.Exchange2013)
                {
                    Credentials = new WebCredentials("hfmgemailservice@hfmg.net", "Top99secret!"),
                    Url = new Uri("https://mail.hfmg.net/EWS/Exchange.asmx")
                };

                service.ImpersonatedUserId = new ImpersonatedUserId(
                    ConnectingIdType.SmtpAddress,
                    request.From
                );

                // ✅ Build subject from form data
                var subject = !string.IsNullOrWhiteSpace(request.FormName)
                    ? $"New Form Request: {request.FormName} — {request.FacilityName}"
                    : request.Subject;

                // ✅ Build HTML body from form submission details
                var body = BuildFormRequestEmailBody(request);

                var email = new EmailMessage(service)
                {
                    Subject = subject,
                    Body = new MessageBody(BodyType.HTML, body)
                };

                if (request.File != null && request.File.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await request.File.CopyToAsync(ms);

                    email.Attachments.AddFileAttachment(
                        request.File.FileName,
                        ms.ToArray()
                    );
                }

                if (toList != null)
                    foreach (var t in toList) email.ToRecipients.Add(t);

                if (ccList != null)
                    foreach (var c in ccList) email.CcRecipients.Add(c);

                email.SendAndSaveCopy();

                return Ok(new { message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending email", error = ex.Message });
            }
        }

        // ✅ Separate method to build the HTML email
        private string BuildFormRequestEmailBody(MailRequest request)
        {
            return $@"
        <html>
        <body style='font-family: Arial, sans-serif; color: #333; padding: 24px;'>
            <h2 style='color: #4f46e5;'>New Form Development Request</h2>
            <p>A new form has been submitted and requires development. Details below:</p>

            <table style='width: 100%; border-collapse: collapse; margin-top: 16px;'>
                <tr style='background: #f5f5f5;'>
                    <td style='padding: 10px 14px; font-weight: bold; width: 180px;'>Form Name</td>
                    <td style='padding: 10px 14px;'>{request.FormName ?? "—"}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 14px; font-weight: bold;'>Facility</td>
                    <td style='padding: 10px 14px;'>{request.FacilityName ?? "—"}</td>
                </tr>
                <tr style='background: #f5f5f5;'>
                    <td style='padding: 10px 14px; font-weight: bold;'>Category</td>
                    <td style='padding: 10px 14px;'>{request.CategoryName ?? "—"}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 14px; font-weight: bold;'>Submitted By</td>
                    <td style='padding: 10px 14px;'>{request.SubmittedBy ?? request.From}</td>
                </tr>
                <tr style='background: #f5f5f5;'>
                    <td style='padding: 10px 14px; font-weight: bold;'>Notes</td>
                    <td style='padding: 10px 14px;'>{(string.IsNullOrWhiteSpace(request.Notes) ? "No notes provided" : request.Notes)}</td>
                </tr>
            </table>

            <p style='margin-top: 24px; color: #888; font-size: 12px;'>
                This email was sent automatically from the Form Automation Dashboard.
            </p>
        </body>
        </html>";
        }



    }
}  

public interface RequestSessionBody
{
    public int patientId { get; set; }

    public string formlabel { get; set; }   
}
