using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Email;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class ForgotUserPasswordUseCase
{
    public const int OtpExpirationMinutes = 15;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwords;
    private readonly IEmailService _email;
    private readonly ILogger<ForgotUserPasswordUseCase> _logger;

    public ForgotUserPasswordUseCase(
        IUserRepository users,
        IPasswordHasher passwords,
        IEmailService email,
        ILogger<ForgotUserPasswordUseCase> logger)
    {
        _users = users;
        _passwords = passwords;
        _email = email;
        _logger = logger;
    }

    public async Task<MessageResponse> HandleAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        const string genericMessage =
            "If an account exists for that email, a reset code has been sent.";

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new MessageResponse(genericMessage);
        }

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        user.PasswordResetOtpHash = _passwords.HashPassword(otp);
        user.PasswordResetOtpExpiresAt = DateTimeOffset.UtcNow.AddMinutes(OtpExpirationMinutes);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _users.UpdateAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        var html = BuildOtpEmailHtml(user.Email, otp, OtpExpirationMinutes);
        var plain = BuildOtpEmailPlainText(user.Email, otp, OtpExpirationMinutes);

        try
        {
            await _email.SendAsync(
                user.Email,
                "ReelBox — password reset code",
                html,
                plain,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset OTP to {Email}", user.Email);
            throw;
        }

        return new MessageResponse(genericMessage);
    }

    private static string BuildOtpEmailPlainText(string accountEmail, string otp, int expiresMinutes) =>
        $"""
        ReelBox — Password reset

        Hi,

        Reset requested for your ReelBox account: {accountEmail}

        Your one-time code is:

        {otp}

        This code expires in {expiresMinutes} minutes.

        If you did not request this, ignore this email.

        ReelBox Support
        support.reelbox@digitaldive.net
        """;

    private static string BuildOtpEmailHtml(string accountEmail, string otp, int expiresMinutes)
    {
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
              <title>ReelBox password reset</title>
            </head>
            <body bgcolor="#F1F5F9" style="margin:0;padding:0;background-color:#F1F5F9;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="#F1F5F9" style="background-color:#F1F5F9;">
                <tr>
                  <td align="center" bgcolor="#F1F5F9" style="padding:28px 12px;background-color:#F1F5F9;">
                    <table role="presentation" width="480" cellspacing="0" cellpadding="0" border="0" bgcolor="#FFFFFF" style="max-width:480px;width:100%;background-color:#FFFFFF;border:1px solid #CBD5E1;">
                      <tr>
                        <td bgcolor="#DD2A7B" style="padding:18px 24px;background-color:#DD2A7B;">
                          <font color="#FFFFFF" face="Arial, Helvetica, sans-serif" size="4"><b>ReelBox</b></font><br />
                          <font color="#FFFFFF" face="Arial, Helvetica, sans-serif" size="2">Password reset</font>
                        </td>
                      </tr>
                      <tr>
                        <td bgcolor="#FFFFFF" style="padding:24px;background-color:#FFFFFF;color:#111111;">
                          <font color="#111111" face="Arial, Helvetica, sans-serif" size="3"><b>Hi,</b></font>
                          <br /><br />
                          <font color="#111111" face="Arial, Helvetica, sans-serif" size="2">
                            Reset requested for your ReelBox account: <b>{safeAccount}</b>
                          </font>
                          <br /><br />
                          <font color="#111111" face="Arial, Helvetica, sans-serif" size="2">
                            Use this one-time code to reset your password. It expires in {expiresMinutes} minutes.
                          </font>
                          <br /><br />
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" bgcolor="#FFFFFF" style="background-color:#FFFFFF;">
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
                            If you did not request this, you can ignore this email. Your password will stay unchanged.
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
