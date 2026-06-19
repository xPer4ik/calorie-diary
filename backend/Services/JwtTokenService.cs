using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CalorieDiary.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace CalorieDiary.Api.Services;

public class JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
{
    public string CreateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = jwtSection["Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        logger.LogInformation("JWT created for user {UserId}.", user.Id);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
