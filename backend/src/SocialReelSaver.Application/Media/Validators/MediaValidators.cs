using FluentValidation;
using SocialReelSaver.Application.Media.DTOs;

namespace SocialReelSaver.Application.Media.Validators;

public sealed class CreateMediaRequestValidator : AbstractValidator<CreateMediaRequest>
{
    public CreateMediaRequestValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.Source)
            .MaximumLength(64)
            .When(x => x.Source is not null);
    }
}
