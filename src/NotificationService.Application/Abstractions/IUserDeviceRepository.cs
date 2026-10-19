using NotificationService.Domain.Entities;
namespace NotificationService.Application.Abstractions;
public interface IUserDeviceRepository
{
    Task<UserDevice?> FindByHashAsync(string hash, CancellationToken ct);
    Task<UserDevice?> FindAsync(Guid id, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserDevice>> ListAsync(Guid userId, bool activeOnly, CancellationToken ct);
    Task AddAsync(UserDevice device, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
