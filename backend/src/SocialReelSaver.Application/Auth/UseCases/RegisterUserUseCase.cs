using SocialReelSaver.Application.Abstractions.Authentication;
using SocialReelSaver.Application.Abstractions.Persistence;
using SocialReelSaver.Application.Auth.DTOs;
using SocialReelSaver.Application.Auth.Mappings;
using SocialReelSaver.Application.Common.Exceptions;
using SocialReelSaver.Domain.Entities;

namespace SocialReelSaver.Application.Auth.UseCases;

public sealed class RegisterUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public RegisterUserUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var accessToken = await _jwtTokenService.CreateAccessTokenAsync(user.Id, user.Email, cancellationToken);
        var refreshToken = await _jwtTokenService.CreateRefreshTokenAsync(cancellationToken);
        var refreshExpiry = _jwtTokenService.GetRefreshTokenExpiry();

        await _refreshTokenService.StoreRefreshTokenAsync(user, refreshToken, refreshExpiry, cancellationToken);
        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(
            accessToken,
            refreshToken,
            _jwtTokenService.GetAccessTokenExpiry(),
            refreshExpiry);
    }
}
