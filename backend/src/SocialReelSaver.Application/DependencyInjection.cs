using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SocialReelSaver.Application.Abstractions.Media;
using SocialReelSaver.Application.Auth.UseCases;
using SocialReelSaver.Application.Media.Services;
using SocialReelSaver.Application.Media.UseCases;

namespace SocialReelSaver.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserUseCase>();

        services.AddSingleton<IMediaUrlAnalyzer, MediaUrlAnalyzer>();

        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        services.AddScoped<LogoutUserUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();

        services.AddScoped<CreateMediaUseCase>();
        services.AddScoped<GetMediaListUseCase>();
        services.AddScoped<GetMediaByIdUseCase>();
        services.AddScoped<RetryMediaUseCase>();
        services.AddScoped<DeleteMediaUseCase>();
        services.AddScoped<GetPlaybackUseCase>();
        services.AddScoped<GetMediaContentUseCase>();

        return services;
    }
}
