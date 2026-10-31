using Microsoft.AspNetCore.Mvc;
using MlhSupport.Application.Customers;

namespace MlhSupport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(CustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerListItemDto>>> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await service.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }
}
