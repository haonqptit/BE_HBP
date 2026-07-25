using FluentValidation;
using HBP.Domain.Enums;

namespace HBP.Application.Admin;

public sealed class SaveRoomTypeRequestValidator : AbstractValidator<SaveRoomTypeRequest>
{
    public SaveRoomTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(150).Matches(AdminSlug.Pattern).WithMessage(AdminSlug.Message);
        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(255);
        RuleFor(x => x.NameJa).MaximumLength(255);
        RuleFor(x => x.ShortDescriptionVi).MaximumLength(500);
        RuleFor(x => x.ShortDescriptionJa).MaximumLength(500);
        RuleFor(x => x.PriceVnd).GreaterThanOrEqualTo(0).When(x => x.PriceVnd.HasValue);
        RuleFor(x => x.PriceUsd).GreaterThanOrEqualTo(0).When(x => x.PriceUsd.HasValue);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.AreaSquareMeters).GreaterThan(0).When(x => x.AreaSquareMeters.HasValue);
        RuleFor(x => x.BedDescriptionVi).MaximumLength(255);
        RuleFor(x => x.BedDescriptionJa).MaximumLength(255);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SeoTitleVi).MaximumLength(255);
        RuleFor(x => x.SeoTitleJa).MaximumLength(255);
        RuleFor(x => x.SeoDescriptionVi).MaximumLength(500);
        RuleFor(x => x.SeoDescriptionJa).MaximumLength(500);
        // A room advertised with SHOW_PRICE must actually carry a price in at least one currency.
        RuleFor(x => x.PriceVnd)
            .Must((request, _) => request.PriceVnd.HasValue || request.PriceUsd.HasValue)
            .When(x => x.PriceDisplayMode == PriceDisplayMode.SHOW_PRICE)
            .WithMessage("priceVnd or priceUsd is required when priceDisplayMode is SHOW_PRICE.");
    }
}
