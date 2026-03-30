using System;
using FormAutomationApi.Context;
using FormAutomationApi.DTOs;
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


        public AdminController(ApplicationDbContext context , ITokenService tokenService,TwilioService twilioService) { 
            _context = context; 
            _tokenService = tokenService;
            _twilioService = twilioService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var allfacilities = await _context.Offices.ToListAsync();
            return Ok(allfacilities);

        }


        // admin create session and sends the data
        [HttpPost("create-session")]
        public async Task<IActionResult> createSession(SendFormRequest body)
        {
            //create a session and send token to frontend
            if (body == null)
            {
                return BadRequest("NO data found form the admin");
            }

            var expiresAt = DateTime.UtcNow.AddHours(24);
            var token = _tokenService.Generate(body, expiresAt);

            return Ok(new { token, expiresAt });
        }

        //adding the twilio service
        [HttpPost("twilio-send")]
        public async Task<IActionResult> SendSms([FromBody] SendForm request)
        {
            if (string.IsNullOrEmpty(request.Phone) || string.IsNullOrEmpty(request.FormLink))
            {
                return BadRequest("Phone or FormLink missing");
            }
            var sent=await _twilioService.SendFormLink(request.Phone, request.FormLink);
            return Ok(new { message = "SMS sent successfully",sent });
        }
    }
}

public interface RequestSessionBody
{
    public int patientId { get; set; }

    public string formlabel { get; set; }   
}
