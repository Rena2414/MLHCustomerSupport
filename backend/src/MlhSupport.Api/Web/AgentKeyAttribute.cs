using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MlhSupport.Infrastructure.Agents;

namespace MlhSupport.Api.Web;

public class AgentKeyAttribute : TypeFilterAttribute
{
    public const string HeaderName = "X-Agent-Key";

    public AgentKeyAttribute() : base(typeof(AgentKeyFilter))
    {
    }

    private class AgentKeyFilter(IOptions<AgentServiceOptions> options) : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var expected = options.Value.ApiKey;
            var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
            if (string.IsNullOrEmpty(expected) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided)))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
