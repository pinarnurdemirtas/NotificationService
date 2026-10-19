using MediatR;
namespace NotificationService.Application.Templates.Commands.CreateTemplate;
public sealed record CreateTemplateCommand(string Code, string Title, string Body) : IRequest<TemplateResponse>;
