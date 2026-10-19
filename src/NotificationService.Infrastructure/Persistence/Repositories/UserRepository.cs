using Microsoft.EntityFrameworkCore;
using Npgsql;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Repositories;
public sealed class UserRepository(NotificationDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == normalizedUsername, ct);
    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking().OrderBy(x => x.Username).Take(100).ToListAsync(ct);
    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new ResourceConflictException("Username is already registered."); }
    }
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
