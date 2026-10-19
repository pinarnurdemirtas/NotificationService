using NotificationService.Domain.Entities;
namespace NotificationService.Application.Abstractions;
public interface INotificationHistoryRepository
{
    Task<Notification?> FindVisibleAsync(Guid id, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Notification>> ListVisibleAsync(Guid userId, int skip, int take, CancellationToken ct);
}
