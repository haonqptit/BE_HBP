namespace HBP.Application.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
public sealed record AdminUserResponse(Guid Id, string Username, string Email);
public sealed record LoginResult(bool Succeeded, bool IsLockedOut, AdminUserResponse? User);
public enum ChangePasswordResult
{
    Succeeded,
    UserNotFound,
    IncorrectCurrentPassword,
    NewPasswordMatchesCurrent
}

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AdminUserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ChangePasswordResult> ChangePasswordAsync(Guid id, ChangePasswordRequest request,
        CancellationToken cancellationToken);
}
