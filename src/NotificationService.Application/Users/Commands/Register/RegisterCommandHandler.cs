using MediatR;
using System.Text.RegularExpressions;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Users.Commands.Register;
public sealed partial class RegisterCommandHandler(IUserRepository users, IUserPasswordHasher hasher, IAccessTokenIssuer tokens) : IRequestHandler<RegisterCommand, AuthResponse>
{
    [GeneratedRegex(@"^[a-zA-Z0-9_.-]{3,50}$")]
    private static partial Regex UsernamePattern();
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || !UsernamePattern().IsMatch(request.Username.Trim()))
            throw new RequestValidationException("Username must be 3-50 characters: letters, digits, dot, underscore or hyphen.");
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 100)
            throw new RequestValidationException("Display name must be 1-100 characters.");
        if (request.Password is null || request.Password.Length is < 10 or > 128)
            throw new RequestValidationException("Password must be 10-128 characters.");
        if (await users.FindByUsernameAsync(request.Username.Trim().ToUpperInvariant(), ct) is not null)
            throw new ResourceConflictException("Username is already registered.");
        var user = User.Create(request.Username, request.DisplayName);
        user.SetPasswordHash(hasher.Hash(user, request.Password));
        await users.AddAsync(user, ct);
        var token = tokens.Issue(user);
        return new(UserResponse.From(user), token.AccessToken, token.ExpiresAtUtc);
    }
}
