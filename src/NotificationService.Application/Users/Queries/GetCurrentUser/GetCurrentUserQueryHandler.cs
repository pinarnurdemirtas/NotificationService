using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Users.Queries.GetCurrentUser;
public sealed class GetCurrentUserQueryHandler(IUserRepository users, ICurrentUser current) : IRequestHandler<GetCurrentUserQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetCurrentUserQuery request, CancellationToken ct) =>
        UserResponse.From(await users.FindByIdAsync(current.GetRequiredUserId(), ct)
            ?? throw new AuthenticationRequiredException("User no longer exists."));
}
