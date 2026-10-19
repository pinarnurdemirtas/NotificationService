namespace NotificationService.Domain.Entities;

public enum PushTargetKind { FcmToken = 0, FirebaseInstallationId = 1 }
public enum NotificationChannel { Push = 0, Sms = 1, Email = 2 }
// Sent means FCM accepted the message, not that the device displayed it.
public enum NotificationStatus { Pending = 0, Sent = 1, Failed = 2, Unknown = 3 }

public sealed class Notification
{
    private Notification() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? SenderUserId { get; private set; }
    public Guid? RecipientUserId { get; private set; }
    public Guid? UserDeviceId { get; private set; }
    public Guid? BatchId { get; private set; }
    public void AssociateUsers(Guid senderId, Guid recipientId, Guid deviceId, Guid batchId)
    { SenderUserId = senderId; RecipientUserId = recipientId; UserDeviceId = deviceId; BatchId = batchId; }
    public Guid? TemplateId { get; private set; }
    public Template? Template { get; private set; }
    public NotificationChannel Channel { get; private set; } = NotificationChannel.Push;
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public PushTargetKind TargetKind { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? DataJson { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; private set; }

    public static Notification CreatePush(string token, string title, string body, Guid? templateId, string? dataJson, PushTargetKind targetKind = PushTargetKind.FcmToken) => new()
    {
        Recipient = token, TargetKind = targetKind, Title = title, Body = body, TemplateId = templateId, DataJson = dataJson
    };

    public void MarkSent(string providerMessageId)
    {
        EnsurePending();
        ProviderMessageId = providerMessageId;
        SentAtUtc = DateTime.UtcNow;
        Status = NotificationStatus.Sent;
    }

    public void MarkFailed(string code, string message, bool outcomeUnknown = false)
    {
        EnsurePending();
        ErrorCode = code;
        ErrorMessage = message;
        Status = outcomeUnknown ? NotificationStatus.Unknown : NotificationStatus.Failed;
    }

    private void EnsurePending()
    {
        if (Status != NotificationStatus.Pending)
            throw new InvalidOperationException("Only pending notifications can be completed.");
    }
}
