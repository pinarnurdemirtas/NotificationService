using MediatR;
namespace NotificationService.Application.Users.Commands.Register;
public sealed record RegisterCommand(string Username, string DisplayName, string Password) : IRequest<AuthResponse>;
