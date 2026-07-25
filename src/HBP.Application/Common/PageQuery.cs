namespace HBP.Application.Common;

/// <summary>
/// Standard admin list query (<c>?page=&amp;pageSize=&amp;search=&amp;sort=</c>).
/// Page size is clamped to 1..100 with a default of 20; the sort key is validated
/// against a per-endpoint whitelist inside the query service.
/// </summary>
public sealed record PageQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Math.Max(Page, 1);
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 100);
    public string? TrimmedSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    public string NormalizedSort => string.IsNullOrWhiteSpace(Sort) ? string.Empty : Sort.Trim().ToLowerInvariant();
}
