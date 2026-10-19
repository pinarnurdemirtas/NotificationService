using MediatR;
namespace NotificationService.Application.Users.Queries.GetCurrentUser;
public sealed record GetCurrentUserQuery : IRequest<UserResponse>;
