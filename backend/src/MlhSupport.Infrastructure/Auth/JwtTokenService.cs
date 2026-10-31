using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public string CreateToken(User user)
    {
        var settings = options.Value;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            [
                new Claim(ClaimNames.Subject, user.Id.ToString()),
                new Claim(ClaimNames.Login, user.Login),
                new Claim(ClaimNames.Name, user.Name),
                new Claim(ClaimNames.Role, user.Role.ToString())
            ],
            expires: DateTime.UtcNow.AddHours(settings.LifetimeHours),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
