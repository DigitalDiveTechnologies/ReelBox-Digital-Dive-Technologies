using FluentValidation;
using SocialReelSaver.Application.AdminAuth.DTOs;

namespace SocialReelSaver.Application.AdminAuth.Validators;

public sealed class AdminLoginRequestValidator : AbstractValidator<AdminLoginRequest>
{
    public AdminLoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}

public sealed class AdminRefreshRequestValidator : AbstractValidator<AdminRefreshRequest>
{
    public AdminRefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
