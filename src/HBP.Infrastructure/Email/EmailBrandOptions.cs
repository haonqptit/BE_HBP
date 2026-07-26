namespace HBP.Infrastructure.Email;

public sealed class EmailBrandOptions
{
    public const string SectionName = "EmailBrand";

    public string WebsiteUrl { get; set; } = "https://bbhomesserviced.com";
    public string LogoUrl { get; set; } = "https://bbhomesserviced.com/Logo.png";
    public string CompanyName { get; set; } = "BB Homes";
    public string Address { get; set; } = "95/12 Đào Tấn, Ba Đình, Hà Nội";
    public string Phone { get; set; } = "084 456 5665";
    public string Email { get; set; } = "admin@bbhomesserviced.com";
    public string? FacebookUrl { get; set; } = "https://www.facebook.com/profile.php?id=61589655256401";
    public string? InstagramUrl { get; set; }
}
