using MediatR;
using NotificationService.Application.Abstractions;
namespace NotificationService.Application.Users.Queries.ListUsers;
public sealed class ListUsersQueryHandler(IUserRepository users, ICurrentUser current) : IRequestHandler<ListUsersQuery, IReadOnlyList<UserResponse>>
{
    public async Task<IReadOnlyList<UserResponse>> Handle(ListUsersQuery request, CancellationToken ct)
    {
        current.GetRequiredUserId();
        return (await users.ListAsync(ct)).Select(UserResponse.From).ToArray();
    }
}
