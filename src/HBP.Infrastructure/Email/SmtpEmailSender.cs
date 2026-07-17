using HBP.Application.Abstractions;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HBP.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(configuration.FromName, configuration.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();
        using var client = new MailKit.Net.Smtp.SmtpClient();
        var security = configuration.Security switch
        {
            "SslOnConnect" => SecureSocketOptions.SslOnConnect,
            "None" => SecureSocketOptions.None,
            _ => SecureSocketOptions.StartTls
        };
        await client.ConnectAsync(configuration.Host, configuration.Port, security, cancellationToken);
        if (!string.IsNullOrWhiteSpace(configuration.Username))
            await client.AuthenticateAsync(configuration.Username, configuration.Password ?? string.Empty, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
