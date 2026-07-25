using FluentValidation;

namespace HBP.Application.Admin;

public sealed record AdminSystemSettingResponse(string Key, string? Value, string? Description, DateTime UpdatedAt);

public sealed record UpdateSystemSettingRequest(string? Value, string? Description);

public sealed class UpdateSystemSettingRequestValidator : AbstractValidator<UpdateSystemSettingRequest>
{
    public UpdateSystemSettingRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(255);
    }
}

public interface IAdminSystemSettingService
{
    Task<IReadOnlyList<AdminSystemSettingResponse>> ListAsync(CancellationToken cancellationToken);
    Task<AdminSystemSettingResponse> GetAsync(string key, CancellationToken cancellationToken);
    Task<AdminSystemSettingResponse> UpdateAsync(string key, UpdateSystemSettingRequest request, CancellationToken cancellationToken);
}
