using FluentValidation;

namespace HBP.Application.Auth;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 128;

    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .MaximumLength(MaximumPasswordLength);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(MinimumPasswordLength)
            .MaximumLength(MaximumPasswordLength)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation does not match.");
    }
}
