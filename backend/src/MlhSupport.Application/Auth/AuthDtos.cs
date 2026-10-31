using MlhSupport.Application.Common;

namespace MlhSupport.Application.Auth;

public record RegisterRequest(string Login, string Password, string Name, int Age, string Role);

public record LoginRequest(string Login, string Password);

public record AuthResponse(string Token, UserDto User);
