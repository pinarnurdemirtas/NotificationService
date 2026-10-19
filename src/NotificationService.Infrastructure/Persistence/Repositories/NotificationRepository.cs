using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Abstractions;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);
    }
    public Task<Notification?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Notifications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Notification>> ListAsync(int skip, int take, CancellationToken cancellationToken) =>
        await db.Notifications.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
