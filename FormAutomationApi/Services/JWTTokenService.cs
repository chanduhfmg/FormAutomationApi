using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FormAutomationApi.DTOs;

namespace FormAutomationApi.Services
{

    public interface ITokenService
    {
        string Generate(SendFormRequest form, DateTime expiresAt); 
         
    }
    public class JWTTokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public JWTTokenService(IConfiguration config) => _config = config;


        // generate token
        public string Generate(SendFormRequest form, DateTime expiresAt)
        {
            var claims = new[]
            {
                new Claim("sub", form.PatientId.ToString()),
        new Claim("patientId", form.PatientId.ToString()),
        new Claim("group", form.Group), // replace with your actual group value
         new Claim("type", "form-session"),
        new Claim("jti", Guid.NewGuid().ToString())
            };

            Console.WriteLine(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));


            var key = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
           issuer: _config["Jwt:Issuer"],
           audience: _config["Jwt:HorizonPatients"],
           claims: claims,
           expires: expiresAt,
           signingCredentials: creds
       );

            return new JwtSecurityTokenHandler().WriteToken(token);
          
        }

        // validate token


        // extract data from the token

    }
}
