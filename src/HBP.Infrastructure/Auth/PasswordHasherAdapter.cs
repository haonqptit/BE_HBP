using HBP.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace HBP.Infrastructure.Auth;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(User, passwordHash, password) != PasswordVerificationResult.Failed;
}
