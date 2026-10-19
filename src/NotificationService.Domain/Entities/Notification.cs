namespace NotificationService.Domain.Entities;
public enum NotificationChannel { Push = 0, Sms = 1, Email = 2 }
public enum NotificationStatus { Pending = 0, Sent = 1, Failed = 2 }
public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TemplateId { get; set; }
    public Template? Template { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Push;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Recipient { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
}
