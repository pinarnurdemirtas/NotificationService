using NotificationService.Domain.Entities;
using NotificationService.Application.Common;
namespace NotificationService.Application.Notifications;

// Device tokens are intentionally not returned by read endpoints.
public sealed record NotificationResponse(
    Guid Id, Guid? TemplateId, NotificationChannel Channel, NotificationStatus Status, PushTargetKind TargetKind,
    string Title, string Body, IReadOnlyDictionary<string, string> Data,
    string? ProviderMessageId, string? ErrorCode, string? ErrorMessage,
    DateTime CreatedAtUtc, DateTime? SentAtUtc,
    Guid? SenderUserId, Guid? RecipientUserId, Guid? UserDeviceId, Guid? BatchId);

public sealed record SendPushResult(NotificationResponse Notification, PushFailureKind? FailureKind);
