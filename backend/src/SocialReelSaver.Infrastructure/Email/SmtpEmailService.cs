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
            _logger.LogError(
                "SMTP config incomplete. HostConfigured={HostConfigured} FromConfigured={FromConfigured} UsernameConfigured={UsernameConfigured} PasswordConfigured={PasswordConfigured} Port={Port} EnableSsl={EnableSsl}",
                !string.IsNullOrWhiteSpace(_options.Host),
                !string.IsNullOrWhiteSpace(_options.FromEmail),
                !string.IsNullOrWhiteSpace(_options.Username),
                !string.IsNullOrWhiteSpace(_options.Password),
                _options.Port,
                _options.EnableSsl);

            throw new InvalidOperationException(
                "SMTP is not configured. Set Smtp:Host and Smtp:FromEmail.");
        }

        if (string.IsNullOrWhiteSpace(_options.Username) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogError(
                "SMTP credentials missing. Host={Host} Port={Port} EnableSsl={EnableSsl} FromEmail={FromEmail} UsernameConfigured={UsernameConfigured} PasswordConfigured={PasswordConfigured}",
                _options.Host,
                _options.Port,
                _options.EnableSsl,
                _options.FromEmail,
                !string.IsNullOrWhiteSpace(_options.Username),
                !string.IsNullOrWhiteSpace(_options.Password));

            throw new InvalidOperationException(
                "SMTP credentials are not configured. Set Smtp:Username and Smtp:Password.");
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
        catch (SmtpException smtpEx)
        {
            _logger.LogError(
                smtpEx,
                "SMTP send failed. To={ToEmail} Host={Host} Port={Port} EnableSsl={EnableSsl} UsernameConfigured={UsernameConfigured} StatusCode={StatusCode} Inner={Inner}",
                toEmail,
                _options.Host,
                _options.Port,
                _options.EnableSsl,
                !string.IsNullOrWhiteSpace(_options.Username),
                smtpEx.StatusCode,
                smtpEx.InnerException?.ToString() ?? "(none)");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP send failed (non-SmtpException). To={ToEmail} Host={Host} Port={Port} EnableSsl={EnableSsl} UsernameConfigured={UsernameConfigured} Inner={Inner}",
                toEmail,
                _options.Host,
                _options.Port,
                _options.EnableSsl,
                !string.IsNullOrWhiteSpace(_options.Username),
                ex.InnerException?.ToString() ?? "(none)");
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
