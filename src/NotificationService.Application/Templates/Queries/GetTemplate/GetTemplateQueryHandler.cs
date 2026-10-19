using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Templates.Queries.GetTemplate;
public sealed class GetTemplateQueryHandler(ITemplateRepository repository) : IRequestHandler<GetTemplateQuery, TemplateResponse>
{
    public async Task<TemplateResponse> Handle(GetTemplateQuery request, CancellationToken cancellationToken) =>
        TemplateMapping.Map(await repository.GetAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Template not found."));
}
