using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FormAutomationApi.Services
{
    public interface ITokenService
    {
        string Generate(RequestToken form, DateTime expiresAt);
        TokenReadResult? Read(string token);
    }

    public class JWTTokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public JWTTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string Generate(RequestToken form, DateTime expiresAt)
        {
            Console.WriteLine($"Generating token for: {form.Account}, {form.Email}, {form.Name}");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  form.Account),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                          DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim("email",   form.Email   ?? ""),
                new Claim("account", form.Account ?? ""),
                new Claim("name",    form.Name    ?? ""),
                new Claim("type",    "form-session"),
                new Claim(ClaimTypes.Role, form.Role ?? "Staff"),
            };

            var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ✅ Bonus — read token without re-validating (already done by [Authorize])
        public TokenReadResult? Read(string rawToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(rawToken);

                return new TokenReadResult
                {
                    Account = jwt.Claims.FirstOrDefault(c => c.Type == "account")?.Value,
                    Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                    Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
                    Type = jwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value,
                    ExpiresAt = jwt.ValidTo,
                    IsExpired = jwt.ValidTo < DateTime.UtcNow,
                };
            }
            catch
            {
                return null;
            }
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class RequestToken
    {
        [Required]
        public string Account { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Staff";
    }

    public class TokenReadResult
    {
        public string? Account { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }
}