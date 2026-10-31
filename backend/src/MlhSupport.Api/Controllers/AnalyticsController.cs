using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MlhSupport.Api.Web;
using MlhSupport.Application.Analytics;
using MlhSupport.Application.Concerns;

namespace MlhSupport.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api")]
public class AnalyticsController(TeacherAnalyticsService analytics, ConcernService concerns) : ControllerBase
{
    [HttpGet("analytics/teachers")]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview(CancellationToken ct) =>
        Ok(await analytics.GetOverviewAsync(ct));

    [HttpGet("analytics/teachers/{id:guid}")]
    public async Task<ActionResult<TeacherAnalyticsDetailsDto>> GetTeacher(Guid id, CancellationToken ct) =>
        Ok(await analytics.GetTeacherAsync(id, ct));

    [HttpPost("analytics/teachers/{id:guid}/summary")]
    public async Task<ActionResult<TeacherSummaryDto>> Summarize(Guid id, CancellationToken ct) =>
        Ok(await analytics.SummarizeAsync(id, ct));

    [HttpPatch("concerns/{id:guid}")]
    public async Task<ActionResult<ConcernDto>> UpdateConcern(Guid id, UpdateConcernRequest request, CancellationToken ct) =>
        Ok(await concerns.UpdateStatusAsync(User.GetUserId(), id, request, ct));
}
