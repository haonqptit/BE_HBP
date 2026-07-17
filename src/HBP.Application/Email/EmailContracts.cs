using HBP.Domain.Enums;

namespace HBP.Application.Email;

public static class EmailTypes
{
    public const string BookingAdminNotification = "BOOKING_ADMIN_NOTIFICATION";
    public const string BookingGuestConfirmation = "BOOKING_GUEST_CONFIRMATION";
    public const string ContactAdminNotification = "CONTACT_ADMIN_NOTIFICATION";
    public const string ContactGuestConfirmation = "CONTACT_GUEST_CONFIRMATION";
}

public sealed record RenderedEmail(string Subject, string HtmlBody);

public interface IEmailTemplateRenderer
{
    Task<RenderedEmail> RenderAsync(string emailType, LanguageCode language,
        IReadOnlyDictionary<string, object?> model, CancellationToken cancellationToken);
}

public static class EmailBackoff
{
    private static readonly TimeSpan[] Delays =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), TimeSpan.FromHours(6)];
    public static TimeSpan? ForAttempt(int attempt) => attempt is >= 1 and <= 5 ? Delays[attempt - 1] : null;
}
