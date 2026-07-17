using FluentValidation;

namespace HBP.Application.Requests;

public sealed class CreateContactRequestValidator : AbstractValidator<CreateContactRequestRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30).Matches(@"^[0-9+\-\s().]{6,30}$");
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
    }
}
