namespace SocialReelSaver.Application.Abstractions.Email;

public interface IEmailService
{
    Task SendHtmlAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default) =>
        SendAsync(toEmail, subject, htmlBody, plainTextBody: null, cancellationToken);

    Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        CancellationToken cancellationToken = default);
}
