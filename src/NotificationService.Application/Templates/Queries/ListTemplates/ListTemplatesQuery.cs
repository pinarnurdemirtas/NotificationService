using MediatR;
namespace NotificationService.Application.Templates.Queries.ListTemplates;
public sealed record ListTemplatesQuery : IRequest<IReadOnlyList<TemplateResponse>>;
