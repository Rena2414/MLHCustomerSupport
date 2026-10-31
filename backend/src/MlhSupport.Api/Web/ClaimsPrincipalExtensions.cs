using System.Security.Claims;
using MlhSupport.Infrastructure.Auth;

namespace MlhSupport.Api.Web;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimNames.Subject), out var id)
            ? id
            : throw new UnauthorizedAccessException("The token has no user id");
}
