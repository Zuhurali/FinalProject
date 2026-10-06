using System.Security.Claims;

namespace FinalProject.MVC.Services.Interfaces;

public interface IApiAccessTokenService
{
    string CreateToken(ClaimsPrincipal user, IEnumerable<string> roles);
}
