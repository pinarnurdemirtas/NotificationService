using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Users.Commands.Login;
public sealed class LoginCommandHandler(IUserRepository users, IUserPasswordHasher hasher, IAccessTokenIssuer tokens) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 50 || request.Password is null || request.Password.Length > 128)
            throw new AuthenticationRequiredException("Username or password is incorrect.");
        var user = await users.FindByUsernameAsync(request.Username.Trim().ToUpperInvariant(), ct);
        if (user is null || !hasher.Verify(user, request.Password))
            throw new AuthenticationRequiredException("Username or password is incorrect.");
        var token = tokens.Issue(user);
        return new(UserResponse.From(user), token.AccessToken, token.ExpiresAtUtc);
    }
}
