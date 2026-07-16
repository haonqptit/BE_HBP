using FluentValidation;

namespace HBP.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(512);
    }
}
