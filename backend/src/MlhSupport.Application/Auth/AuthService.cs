using System.Text.RegularExpressions;
using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Auth;

public partial class AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens, IAuditLog audit)
{
    public const int MinPasswordLength = 4;
    public const int MaxNameLength = 100;

    private static readonly UserRole[] SelfRegisterRoles = [UserRole.Teacher, UserRole.Student, UserRole.Parent];

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var login = NormalizeLogin(request.Login);
        if (!LoginPattern().IsMatch(login))
        {
            throw new ValidationException("Login must be 3 to 40 characters: letters, digits, dot, dash or underscore");
        }
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < MinPasswordLength)
        {
            throw new ValidationException($"Password must be at least {MinPasswordLength} characters");
        }
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > MaxNameLength)
        {
            throw new ValidationException($"Name must be between 1 and {MaxNameLength} characters");
        }
        if (request.Age < User.MinAge || request.Age > User.MaxAge)
        {
            throw new ValidationException($"Age must be between {User.MinAge} and {User.MaxAge}");
        }
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) || !SelfRegisterRoles.Contains(role))
        {
            throw new ValidationException("Role must be teacher, student or parent");
        }
        if (await users.GetByLoginAsync(login, ct) is not null)
        {
            throw new ConflictException("This login is already taken");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = hasher.Hash(request.Password),
            Name = name,
            Age = request.Age,
            Role = role,
            SubscriptionType = SubscriptionType.None,
            CreatedAt = DateTime.UtcNow
        };
        await users.AddAsync(user, ct);
        await audit.WriteAsync(Audit.Entry(user, "user.registered", user.Id, $"Registered as {role}"), ct);
        return new AuthResponse(tokens.CreateToken(user), user.ToDto());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByLoginAsync(NormalizeLogin(request.Login), ct);
        if (user is null || string.IsNullOrEmpty(request.Password) || !hasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Wrong login or password");
        }
        return new AuthResponse(tokens.CreateToken(user), user.ToDto());
    }

    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User not found");
        return user.ToDto();
    }

    public static string NormalizeLogin(string? login) => (login ?? string.Empty).Trim().ToLowerInvariant();

    [GeneratedRegex("^[a-z0-9._-]{3,40}$")]
    private static partial Regex LoginPattern();
}
