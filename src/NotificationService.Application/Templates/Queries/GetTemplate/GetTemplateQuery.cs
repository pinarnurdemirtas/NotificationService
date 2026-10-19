using MediatR;
namespace NotificationService.Application.Templates.Queries.GetTemplate;
public sealed record GetTemplateQuery(Guid Id) : IRequest<TemplateResponse>;
