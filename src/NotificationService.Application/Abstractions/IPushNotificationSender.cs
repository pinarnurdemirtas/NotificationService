using NotificationService.Domain.Entities;
namespace NotificationService.Application.Abstractions;

public sealed record PushMessage(string Target, string Title, string Body, IReadOnlyDictionary<string, string> Data, PushTargetKind TargetKind = PushTargetKind.FcmToken);

public interface IPushNotificationSender
{
    Task<string> SendAsync(PushMessage message, CancellationToken cancellationToken = default);
}
