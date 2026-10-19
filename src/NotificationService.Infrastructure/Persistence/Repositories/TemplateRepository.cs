using Microsoft.EntityFrameworkCore;
using Npgsql;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
namespace NotificationService.Infrastructure.Persistence.Repositories;

public sealed class TemplateRepository(NotificationDbContext db) : ITemplateRepository
{
    public Task<Template?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Templates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        db.Templates.AnyAsync(x => x.Code == code, cancellationToken);
    public async Task<IReadOnlyList<Template>> ListAsync(CancellationToken cancellationToken) =>
        await db.Templates.AsNoTracking().OrderBy(x => x.Code).Take(100).ToListAsync(cancellationToken);
    public async Task AddAsync(Template template, CancellationToken cancellationToken)
    {
        db.Templates.Add(template);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new ResourceConflictException("A template with this code already exists."); }
    }
}
