using MlhSupport.Application.Auth;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Tests;

public class AuthServiceTests
{
    private static (AuthService Service, InMemoryStore Store) Build()
    {
        var store = new InMemoryStore();
        return (new AuthService(store, new FakeHasher(), new FakeTokens(), store), store);
    }

    [Fact]
    public async Task Register_CreatesUserWithoutSubscriptionAndReturnsToken()
    {
        var (service, store) = Build();

        var result = await service.RegisterAsync(new RegisterRequest(" Emma ", "secret", "Emma", 10, "student"), CancellationToken.None);

        Assert.Equal("token:emma", result.Token);
        Assert.Equal("Student", result.User.Role);
        Assert.Equal(0, result.User.SubscriptionType);
        Assert.Equal("hash:secret", Assert.Single(store.Users).PasswordHash);
        Assert.Single(store.AuditEntries);
    }

    [Fact]
    public async Task Register_DuplicateLogin_Conflicts()
    {
        var (service, _) = Build();
        await service.RegisterAsync(new RegisterRequest("emma", "secret", "Emma", 10, "student"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.RegisterAsync(new RegisterRequest("EMMA", "other1", "Emma 2", 11, "student"), CancellationToken.None));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("superuser")]
    public async Task Register_AdminOrUnknownRole_IsRejected(string role)
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(new RegisterRequest("boss", "secret", "Boss", 30, role), CancellationToken.None));
    }

    [Theory]
    [InlineData("ab", "secret", "Name", 10)]
    [InlineData("valid", "abc", "Name", 10)]
    [InlineData("valid", "secret", "", 10)]
    [InlineData("valid", "secret", "Name", 2)]
    public async Task Register_InvalidInput_IsRejected(string login, string password, string name, int age)
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.RegisterAsync(new RegisterRequest(login, password, name, age, "parent"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_ChecksPassword()
    {
        var (service, store) = Build();
        store.AddUser("admin", UserRole.Admin);

        var result = await service.LoginAsync(new LoginRequest("Admin", "admin"), CancellationToken.None);

        Assert.Equal("Admin", result.User.Role);
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.LoginAsync(new LoginRequest("nobody", "admin"), CancellationToken.None));
    }
}
