using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MlhSupport.Api.Web;
using MlhSupport.Application.Classes;

namespace MlhSupport.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/classes")]
public class ClassesController(ClassService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher},{Roles.Student}")]
    public async Task<ActionResult<IReadOnlyList<ClassDto>>> GetAll(CancellationToken ct) =>
        Ok(await service.GetVisibleAsync(User.GetUserId(), ct));

    [HttpPost]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ClassDto>> Create(CreateClassRequest request, CancellationToken ct) =>
        Ok(await service.CreateAsync(User.GetUserId(), request, ct));

    [HttpPost("{id:guid}/students")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ClassDto>> Enroll(Guid id, EnrollStudentRequest request, CancellationToken ct) =>
        Ok(await service.EnrollAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("{id:guid}/students/{studentId:guid}")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> Remove(Guid id, Guid studentId, CancellationToken ct)
    {
        await service.RemoveAsync(User.GetUserId(), id, studentId, ct);
        return NoContent();
    }
}
