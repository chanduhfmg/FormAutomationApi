using FormAutomationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FormAutomationApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {

        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;

        public AuthController(HttpClient httpClient, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request");
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("http://172.22.6.145/login", request);
                Console.WriteLine(response);

                if (response.IsSuccessStatusCode)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    var content = JsonSerializer.Deserialize<LoginApiResponse>(contentString);

                    var token = _tokenService.Generate(new RequestToken
                    {
                        Account = content.Account,
                        Email = content.Email,
                        Name = content.Name
                    }, DateTime.UtcNow.AddHours(24));
                    return Ok(token);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(error);

                    return StatusCode((int)response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error during login", error = ex.Message });
            }


            // Implement your login logic here

        }

        [HttpGet("login")]
        public async Task<IActionResult> Get()
        {
            var token = Request.Headers["Authorization"]
           .ToString()
           .Replace("Bearer ", "");

            var tokenData = _tokenService.Read(token);


            return Ok(tokenData);
        }
    }
}

public class LoginRequest
{
    public string email { get; set; }
    public string Password { get; set; }
}

public class LoginApiResponse
{
    [JsonPropertyName("account")]
    public string Account { get; set; }

    [JsonPropertyName("mail")]  // map "mail" to this property
    public string Email { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }
}
