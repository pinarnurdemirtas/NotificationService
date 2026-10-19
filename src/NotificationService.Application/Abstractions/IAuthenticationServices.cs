using NotificationService.Domain.Entities;
namespace NotificationService.Application.Abstractions;
public interface ICurrentUser { Guid GetRequiredUserId(); }
public interface IUserPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}
public sealed record IssuedAccessToken(string AccessToken, DateTime ExpiresAtUtc);
public interface IAccessTokenIssuer { IssuedAccessToken Issue(User user); }
