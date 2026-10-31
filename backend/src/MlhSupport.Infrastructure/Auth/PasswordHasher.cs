using Microsoft.AspNetCore.Identity;
using MlhSupport.Application.Abstractions;

namespace MlhSupport.Infrastructure.Auth;

public class PasswordHasher : IPasswordHasher
{
    private static readonly object Subject = new();
    private readonly PasswordHasher<object> inner = new();

    public string Hash(string password) => inner.HashPassword(Subject, password);

    public bool Verify(string hash, string password) =>
        inner.VerifyHashedPassword(Subject, hash, password) != PasswordVerificationResult.Failed;
}
