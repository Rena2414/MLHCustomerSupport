using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MlhSupport.Api.Web;
using MlhSupport.Application.Concerns;
using MlhSupport.Application.Students;
using MlhSupport.Application.Subscriptions;

namespace MlhSupport.Api.Controllers;

[ApiController]
[AllowAnonymous]
[AgentKey]
[Route("api/internal")]
public class InternalController(
    StudentContextService students,
    SubscriptionService subscriptions,
    ConcernService concerns) : ControllerBase
{
    [HttpGet("students/{id:guid}/context")]
    public async Task<ActionResult<StudentContextDto>> GetStudentContext(Guid id, CancellationToken ct) =>
        Ok(await students.GetAsync(id, ct));

    [HttpPost("students/{id:guid}/subscription/cancel")]
    public async Task<ActionResult<SubscriptionChangeDto>> CancelSubscription(Guid id, CancellationToken ct) =>
        Ok(await subscriptions.CancelByAgentAsync(id, ct));

    [HttpPost("concerns")]
    public async Task<ActionResult<ConcernDto>> ReportConcern(ReportConcernRequest request, CancellationToken ct) =>
        Ok(await concerns.ReportByAgentAsync(request, ct));
}
