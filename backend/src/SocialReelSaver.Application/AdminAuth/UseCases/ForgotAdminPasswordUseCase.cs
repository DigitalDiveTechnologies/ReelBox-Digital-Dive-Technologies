using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Email;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.AdminAuth.DTOs;
using SocialReelSaver.Shared.Configuration;

namespace SocialReelSaver.Application.AdminAuth.UseCases;

public sealed class ForgotAdminPasswordUseCase
{
    public const int OtpExpirationMinutes = 15;

    private readonly IAdminUserRepository _admins;
    private readonly IPasswordHasher _passwords;
    private readonly IEmailService _email;
    private readonly SmtpOptions _smtp;
    private readonly ILogger<ForgotAdminPasswordUseCase> _logger;

    public ForgotAdminPasswordUseCase(
        IAdminUserRepository admins,
        IPasswordHasher passwords,
        IEmailService email,
        IOptions<SmtpOptions> smtp,
        ILogger<ForgotAdminPasswordUseCase> logger)
    {
        _admins = admins;
        _passwords = passwords;
        _email = email;
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task<AdminMessageResponse> HandleAsync(
        AdminForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        const string genericMessage =
            "If an account exists for that email, a reset code has been sent.";

        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await _admins.GetByEmailAsync(email, cancellationToken);

        if (admin is null || !admin.IsActive)
        {
            return new AdminMessageResponse(genericMessage);
        }

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        admin.PasswordResetOtpHash = _passwords.HashPassword(otp);
        admin.PasswordResetOtpExpiresAt = DateTimeOffset.UtcNow.AddMinutes(OtpExpirationMinutes);
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        await _admins.UpdateAsync(admin, cancellationToken);
        await _admins.SaveChangesAsync(cancellationToken);

        var deliverTo = ResolveNotifyInbox();
        var accountLabel = admin.DisplayName ?? admin.Email;
        var html = BuildOtpEmailHtml(accountLabel, admin.Email, otp, OtpExpirationMinutes);
        var plain = BuildOtpEmailPlainText(accountLabel, admin.Email, otp, OtpExpirationMinutes);

        try
        {
            await _email.SendAsync(
                deliverTo,
                "ReelBox Admin — password reset code",
                html,
                plain,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset OTP to {Email}", deliverTo);
            throw;
        }

        return new AdminMessageResponse(genericMessage);
    }

    private string ResolveNotifyInbox()
    {
        if (!string.IsNullOrWhiteSpace(_smtp.ResetNotifyEmail))
        {
            return _smtp.ResetNotifyEmail.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_smtp.FromEmail))
        {
            return _smtp.FromEmail.Trim();
        }

        return "support.reelbox@digitaldive.net";
    }

    private static string BuildOtpEmailPlainText(
        string displayName,
        string accountEmail,
        string otp,
        int expiresMinutes) =>
        $"""
        ReelBox Admin — Password reset

        Hi {displayName},

        Reset requested for admin account: {accountEmail}

        Your one-time code is:

        {otp}

        This code expires in {expiresMinutes} minutes.

        If you did not request this, ignore this email.

        ReelBox Support
        support.reelbox@digitaldive.net
        """;

    /// <summary>
    /// Dark-mode-safe HTML: solid white card + near-black text via bgcolor/font tags
    /// (MXRoute / Gmail / Outlook often force white CSS text in dark themes).
    /// </summary>
    private static string BuildOtpEmailHtml(
        string displayName,
        string accountEmail,
        string otp,
        int expiresMinutes)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(displayName);
        var safeAccount = System.Net.WebUtility.HtmlEncode(accountEmail);
        var safeOtp = System.Net.WebUtility.HtmlEncode(otp);

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <meta name="color-scheme" content="light only" />
              <meta name="supported-color-schemes" content="light only" />
              <title>ReelBox Admin password reset</title>
            </head>
            <body bgcolor="#F1F5F9" style="margin:0;padding:0;background-color:#F1F5F9;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="#F1F5F9" style="background-color:#F1F5F9;">
                <tr>
                  <td align="center" bgcolor="#F1F5F9" style="padding:28px 12px;background-color:#F1F5F9;">
                    <table role="presentation" width="480" cellspacing="0" cellpadding="0" border="0" bgcolor="#FFFFFF" style="max-width:480px;width:100%;background-color:#FFFFFF;border:1px solid #CBD5E1;">
                      <tr class="header">
                        <td bgcolor="#DD2A7B" style="padding:18px 24px;background-color:#DD2A7B;">
                          <font color="#FFFFFF" face="Arial, Helvetica, sans-serif" size="4"><b>ReelBox Admin</b></font><br />
                          <font color="#FFFFFF" face="Arial, Helvetica, sans-serif" size="2">Password reset</font>
                        </td>
                      </tr>
                      <tr>
                        <td bgcolor="#FFFFFF" style="padding:24px;background-color:#FFFFFF;color:#111111;">
                          <font color="#111111" face="Arial, Helvetica, sans-serif" size="3">
                            <b>Hi {safeName},</b>
                          </font>
                          <br /><br />
                          <font color="#111111" face="Arial, Helvetica, sans-serif" size="2">
                            Reset requested for admin account: <b>{safeAccount}</b>
                          </font>
                          <br /><br />
                          <font color="#111111" face="Arial, Helvetica, sans-serif" size="2">
                            Use this one-time code to reset the password. It expires in {expiresMinutes} minutes.
                          </font>
                          <br /><br />
                          <table role="presentation" class="otp-box" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="#FFFFFF" style="background-color:#FFFFFF;">
                            <tr>
                              <td align="center" bgcolor="#FFFFFF" style="background-color:#FFFFFF;padding:8px 0;">
                                <table role="presentation" cellspacing="0" cellpadding="0" border="0" bgcolor="#FFFFFF" style="background-color:#FFFFFF;border:2px solid #111111;">
                                  <tr>
                                    <td align="center" bgcolor="#FFFFFF" style="padding:18px 26px;background-color:#FFFFFF;">
                                      <font color="#111111" face="Consolas, Courier New, monospace" style="font-size:32px;font-weight:700;letter-spacing:6px;color:#111111;">
                                        <b>{safeOtp}</b>
                                      </font>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                          </table>
                          <br />
                          <font color="#333333" face="Arial, Helvetica, sans-serif" size="1">
                            If you did not request this, you can ignore this email. The password will stay unchanged.
                          </font>
                        </td>
                      </tr>
                      <tr>
                        <td bgcolor="#FFFFFF" style="padding:14px 24px 20px;background-color:#FFFFFF;border-top:1px solid #E2E8F0;">
                          <font color="#333333" face="Arial, Helvetica, sans-serif" size="1">
                            ReelBox Support · support.reelbox@digitaldive.net
                          </font>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
