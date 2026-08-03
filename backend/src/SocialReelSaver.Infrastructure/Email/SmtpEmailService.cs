using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Email;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Infrastructure.Email;

/// <summary>
/// MXRoute / SMTP delivery via <see cref="SmtpClient"/> (Admin notifications).
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendHtmlAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, subject, htmlBody, plainTextBody: null, cancellationToken);

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set Smtp:Host and Smtp:FromEmail.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                _options.FromEmail,
                string.IsNullOrWhiteSpace(_options.FromName) ? _options.FromEmail : _options.FromName),
            Subject = subject,
        };
        message.To.Add(toEmail.Trim());

        var plain = string.IsNullOrWhiteSpace(plainTextBody)
            ? StripTagsFallback(htmlBody)
            : plainTextBody;

        // Multipart/alternative: plain text first (always readable), HTML second.
        var plainView = AlternateView.CreateAlternateViewFromString(
            plain,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain);
        var htmlView = AlternateView.CreateAlternateViewFromString(
            htmlBody,
            Encoding.UTF8,
            MediaTypeNames.Text.Html);
        message.AlternateViews.Add(plainView);
        message.AlternateViews.Add(htmlView);
        message.Body = plain;
        message.IsBodyHtml = false;

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("SMTP email sent to {ToEmail} subject={Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMTP email to {ToEmail}", toEmail);
            throw;
        }
    }

    private static string StripTagsFallback(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(html.Length);
        var inside = false;
        foreach (var ch in html)
        {
            if (ch == '<')
            {
                inside = true;
                continue;
            }

            if (ch == '>')
            {
                inside = false;
                continue;
            }

            if (!inside)
            {
                sb.Append(ch);
            }
        }

        return WebUtility.HtmlDecode(sb.ToString());
    }
}
