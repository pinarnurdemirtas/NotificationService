using MediatR;
namespace NotificationService.Application.Users.Commands.Login;
public sealed record LoginCommand(string Username, string Password) : IRequest<AuthResponse>;
