using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MlhSupport.Api.Web;
using MlhSupport.Application.Auth;
using MlhSupport.Application.Classes;
using MlhSupport.Application.Common;
using MlhSupport.Application.Families;
using MlhSupport.Application.Subscriptions;

namespace MlhSupport.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController(
    AuthService auth,
    SubscriptionService subscriptions,
    FamilyService families,
    ClassService classes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserDto>> Get(CancellationToken ct) =>
        Ok(await auth.GetMeAsync(User.GetUserId(), ct));

    [HttpPost("subscription")]
    [Authorize(Roles = $"{Roles.Student},{Roles.Parent}")]
    public async Task<ActionResult<UserDto>> Subscribe(SubscribeRequest? request, CancellationToken ct) =>
        Ok(await subscriptions.SubscribeAsync(User.GetUserId(), request ?? new SubscribeRequest(null), ct));

    [HttpGet("classes")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<IReadOnlyList<ClassDto>>> GetClasses(CancellationToken ct) =>
        Ok(await classes.GetForStudentAsync(User.GetUserId(), ct));

    [HttpGet("children")]
    [Authorize(Roles = Roles.Parent)]
    public async Task<ActionResult<IReadOnlyList<ChildDto>>> GetChildren(CancellationToken ct) =>
        Ok(await families.GetChildrenAsync(User.GetUserId(), ct));

    [HttpPost("children")]
    [Authorize(Roles = Roles.Parent)]
    public async Task<ActionResult<ChildDto>> LinkChild(LinkChildRequest request, CancellationToken ct) =>
        Ok(await families.LinkChildAsync(User.GetUserId(), request, ct));

    [HttpDelete("children/{childId:guid}")]
    [Authorize(Roles = Roles.Parent)]
    public async Task<IActionResult> UnlinkChild(Guid childId, CancellationToken ct)
    {
        await families.UnlinkChildAsync(User.GetUserId(), childId, ct);
        return NoContent();
    }
}
