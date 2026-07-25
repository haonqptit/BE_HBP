using FluentValidation;

namespace HBP.Application.Admin;

/// <summary>Compact media projection reused by every admin content DTO.</summary>
public sealed record AdminMediaSummary(Guid Id, string PublicUrl, string MediumUrl, string ThumbnailUrl,
    string? AltTextVi, string? AltTextJa);

/// <summary>One entry of a replace-set payload that carries an explicit ordering.</summary>
public sealed record OrderedLinkRequest(Guid Id, int? DisplayOrder);

public sealed record ReplaceLinksRequest(IReadOnlyList<OrderedLinkRequest> Items);

public sealed class ReplaceLinksRequestValidator : AbstractValidator<ReplaceLinksRequest>
{
    public ReplaceLinksRequestValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id).NotEmpty();
            item.RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
        });
    }
}
