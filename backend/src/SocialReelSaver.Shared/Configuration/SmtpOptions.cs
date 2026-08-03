namespace SocialReelSaver.Shared.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "ReelBox Support";

    /// <summary>
    /// Inbox that receives admin password-reset OTP emails.
    /// Defaults to <see cref="FromEmail"/> when empty.
    /// </summary>
    public string ResetNotifyEmail { get; set; } = string.Empty;
}
