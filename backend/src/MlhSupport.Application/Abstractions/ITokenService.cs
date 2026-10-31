using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface ITokenService
{
    string CreateToken(User user);
}
