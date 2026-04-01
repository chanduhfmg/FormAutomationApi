using System;
using FormAutomationApi.Context;
using FormAutomationApi.DTOs;
using FormAutomationApi.Model;
using FormAutomationApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var allfacilities = await _context.Offices.ToListAsync();
            return Ok(allfacilities);

        }



        //adding the twilio service
        [HttpPost("twilio-send")]
        public async Task<IActionResult> SendSms([FromBody] SendForm request)
        {
            if (string.IsNullOrEmpty(request.Phone) || string.IsNullOrEmpty(request.FormLink))
            {
                return BadRequest("Phone or FormLink missing");
            }

            try
            {
                var submission = new FormSubmission
                {
                    CreatedAt = DateTime.Now,
                    PatientId = int.TryParse(request.patientId, out var pid) ? pid : null,
                    ExpiresAt = DateTime.Now.AddDays(7),
                    FormIds = request.FormLink,
                    SenderId = 0
                };

                await _context.FormSubmissions.AddAsync(submission);

                // ⚡ This actually saves it to the DB
                await _context.SaveChangesAsync();

                //create form link in this
                var baseUrl = _config["Frontend:FormsUrl"];
                string formLink = $"{baseUrl}/forms?token={submission.SessionId}";


                var sent = await _twilioService.SendFormLink(request.Phone, formLink);
                return Ok(new { message = "SMS sent successfully", sent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending SMS", error = ex.Message });

            }
        }

        [HttpGet("get-sessions")]
        public async Task<IActionResult> GetSessions()
        {
            // Logic to retrieve sessions from the database
            var sessions = await _context.FormSubmissions.ToListAsync();

            var expiredSessions = sessions.Where(s => s.ExpiresAt < DateTime.Now && s.Status != SubmissionStatus.Completed).ToList();
            if (expiredSessions.Any())
            {
                expiredSessions.ForEach(s=>s.Status = SubmissionStatus.Expired);
                await _context.SaveChangesAsync();
            }
            return Ok(sessions);
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
    }
}  

public interface RequestSessionBody
{
    public int patientId { get; set; }

    public string formlabel { get; set; }   
}
