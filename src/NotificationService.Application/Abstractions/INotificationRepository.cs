using NotificationService.Domain.Entities;
namespace NotificationService.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> ListAsync(int skip, int take, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
