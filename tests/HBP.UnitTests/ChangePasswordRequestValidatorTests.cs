using HBP.Application.Auth;

namespace HBP.UnitTests;

public sealed class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void AcceptsMatchingStrongPassword() =>
        Assert.True(_validator.Validate(new ChangePasswordRequest(
            "Current-Password-2026!", "New-Password-2026!", "New-Password-2026!")).IsValid);

    [Fact]
    public void RejectsShortPassword()
    {
        var result = _validator.Validate(new ChangePasswordRequest("Current-Password-2026!", "short", "short"));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void RejectsMismatchedConfirmation()
    {
        var result = _validator.Validate(new ChangePasswordRequest(
            "Current-Password-2026!", "New-Password-2026!", "Different-Password-2026!"));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(ChangePasswordRequest.ConfirmPassword));
    }
}
