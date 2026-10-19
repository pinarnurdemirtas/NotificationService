using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Repositories;
public sealed class NotificationHistoryRepository(NotificationDbContext db) : INotificationHistoryRepository
{
    public Task<Notification?> FindVisibleAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.Notifications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && (x.SenderUserId == userId || x.RecipientUserId == userId), ct);
    public async Task<IReadOnlyList<Notification>> ListVisibleAsync(Guid userId, int skip, int take, CancellationToken ct) =>
        await db.Notifications.AsNoTracking().Where(x => x.SenderUserId == userId || x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip(skip).Take(take).ToListAsync(ct);
}
