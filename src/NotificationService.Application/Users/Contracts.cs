using NotificationService.Domain.Entities;
namespace NotificationService.Application.Users;
public sealed record UserResponse(Guid Id, string Username, string DisplayName)
{
    public static UserResponse From(User user) => new(user.Id, user.Username, user.DisplayName);
}
public sealed record AuthResponse(UserResponse User, string AccessToken, DateTime ExpiresAtUtc);
