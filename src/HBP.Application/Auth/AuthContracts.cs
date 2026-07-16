namespace HBP.Application.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record AdminUserResponse(Guid Id, string Username, string Email);
public sealed record LoginResult(bool Succeeded, bool IsLockedOut, AdminUserResponse? User);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AdminUserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
