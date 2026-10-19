using NotificationService.Domain.Entities;
namespace NotificationService.Application.Abstractions;

public interface ITemplateRepository
{
    Task<Template?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<Template>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(Template template, CancellationToken cancellationToken);
}
