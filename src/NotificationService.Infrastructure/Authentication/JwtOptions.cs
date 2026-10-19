namespace NotificationService.Infrastructure.Authentication;
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "NotificationService";
    public string Audience { get; set; } = "NotificationService.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeMinutes { get; set; } = 60;
}
