using Microsoft.EntityFrameworkCore;
using Npgsql;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Repositories;
public sealed class UserDeviceRepository(NotificationDbContext db) : IUserDeviceRepository
{
    public Task<UserDevice?> FindByHashAsync(string hash, CancellationToken ct) => db.UserDevices.Where(x => x.TokenHash == hash).OrderByDescending(x => x.IsActive).ThenByDescending(x => x.LastSeenAtUtc).FirstOrDefaultAsync(ct);
    public Task<UserDevice?> FindAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.UserDevices.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    public async Task<IReadOnlyList<UserDevice>> ListAsync(Guid userId, bool activeOnly, CancellationToken ct) =>
        await db.UserDevices.Where(x => x.UserId == userId && (!activeOnly || x.IsActive)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
    public async Task AddAsync(UserDevice device, CancellationToken ct)
    {
        db.UserDevices.Add(device);
        await SaveChangesAsync(ct);
    }
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new ResourceConflictException("Device was registered concurrently. Refresh and try again."); }
    }
}
