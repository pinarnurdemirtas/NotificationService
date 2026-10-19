namespace NotificationService.Domain.Entities;
public sealed class UserDevice
{
    private UserDevice() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PushTargetKind TargetKind { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; private set; } = DateTime.UtcNow;
    public static UserDevice Create(Guid userId, string token, string hash, string name, PushTargetKind kind) => new()
    { UserId = userId, Token = token, TokenHash = hash, Name = name, TargetKind = kind };
    public void Register(Guid userId, string name)
    {
        if (UserId != userId) throw new InvalidOperationException("Active device belongs to another user.");
        UserId = userId; Name = name; IsActive = true; LastSeenAtUtc = DateTime.UtcNow;
    }
    public void Deactivate() => IsActive = false;
}
