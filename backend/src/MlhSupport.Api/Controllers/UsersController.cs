using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MlhSupport.Api.Web;
using MlhSupport.Application.Common;
using MlhSupport.Application.Users;

namespace MlhSupport.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/users")]
public class UsersController(UserService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll([FromQuery] string? role, CancellationToken ct) =>
        Ok(await service.GetAllAsync(role, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailsDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await service.GetDetailsAsync(id, ct));
}
