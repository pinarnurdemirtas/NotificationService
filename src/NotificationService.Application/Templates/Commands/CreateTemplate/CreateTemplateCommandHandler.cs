using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Templates.Commands.CreateTemplate;

public sealed class CreateTemplateCommandHandler(ITemplateRepository repository) : IRequestHandler<CreateTemplateCommand, TemplateResponse>
{
    public async Task<TemplateResponse> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 3000)
            throw new RequestValidationException("Code (1-100), title (1-200), and body (1-3000) are required.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await repository.CodeExistsAsync(code, cancellationToken))
            throw new ResourceConflictException("A template with this code already exists.");
        var template = new Template { Code = code, Title = request.Title.Trim(), Body = request.Body.Trim() };
        await repository.AddAsync(template, cancellationToken);
        return TemplateMapping.Map(template);
    }
}
