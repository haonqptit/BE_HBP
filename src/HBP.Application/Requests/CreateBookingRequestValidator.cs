using FluentValidation;

namespace HBP.Application.Requests;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequestRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30).Matches(@"^[0-9+\-\s().]{6,30}$");
        RuleFor(x => x.Adults).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0).When(x => x.Children.HasValue);
        RuleFor(x => x.NumberOfRooms).GreaterThanOrEqualTo(1).When(x => x.NumberOfRooms.HasValue);
        RuleFor(x => x.CustomerMessage).MaximumLength(4000);
        // BR-BOOK-014 / FR-BOOK-003: intentionally do NOT validate check-out > check-in for MVP.
    }
}
