using MediatR;
namespace NotificationService.Application.Users.Queries.ListUsers;
public sealed record ListUsersQuery : IRequest<IReadOnlyList<UserResponse>>;
