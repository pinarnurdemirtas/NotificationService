using System.Text.Json;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Notifications;

internal static class NotificationMapping
{
public static NotificationResponse Map(Notification n) => new(n.Id, n.TemplateId, n.Channel, n.Status, n.TargetKind,
        n.Title, n.Body, n.DataJson is null ? new Dictionary<string, string>() :
            JsonSerializer.Deserialize<Dictionary<string, string>>(n.DataJson)!,
        n.ProviderMessageId, n.ErrorCode, n.ErrorMessage, n.CreatedAtUtc, n.SentAtUtc, n.SenderUserId, n.RecipientUserId, n.UserDeviceId, n.BatchId);
}
