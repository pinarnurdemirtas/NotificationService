using MediatR;
using NotificationService.Application.Abstractions;
namespace NotificationService.Application.Templates.Queries.ListTemplates;
public sealed class ListTemplatesQueryHandler(ITemplateRepository repository) : IRequestHandler<ListTemplatesQuery, IReadOnlyList<TemplateResponse>>
{
    public async Task<IReadOnlyList<TemplateResponse>> Handle(ListTemplatesQuery request, CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(TemplateMapping.Map).ToArray();
}
