using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinalProject.MVC.Security;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace FinalProject.MVC.Services;

public sealed class ApiAccessTokenService : IApiAccessTokenService
{
    private readonly IConfiguration _configuration;

    public ApiAccessTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(ClaimsPrincipal user, IEnumerable<string> roles)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("An authenticated user id is required.");
        }

        var signingKey = ApiSigningKey.RequireConfigured(_configuration);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, user.Identity?.Name ?? userId)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["ApiAuthentication:Issuer"],
            audience: _configuration["ApiAuthentication:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
