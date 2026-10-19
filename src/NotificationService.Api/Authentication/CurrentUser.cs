using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Api.Authentication;
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid GetRequiredUserId()
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true || !Guid.TryParse(user.FindFirst("sub")?.Value, out var id))
            throw new AuthenticationRequiredException("Sign in to continue.");
        return id;
    }
}
