namespace NotificationService.Domain.Entities;
public sealed class User
{
    private User() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public static User Create(string username, string displayName) => new()
    {
        Username = username.Trim().ToLowerInvariant(), NormalizedUsername = username.Trim().ToUpperInvariant(),
        DisplayName = displayName.Trim()
    };
    public void SetPasswordHash(string hash) => PasswordHash = hash;
}
