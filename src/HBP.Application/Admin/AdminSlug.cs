namespace HBP.Application.Admin;

/// <summary>Slug rules shared by every admin content validator.</summary>
public static class AdminSlug
{
    public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";
    public const string Message = "Slug must contain lower-case letters, digits and single hyphens only.";
}
