using MediatR;
namespace NotificationService.Application.Notifications.Commands.SendUserPush;
// The sender is deliberately not part of the API contract.
public sealed record SendUserPushCommand(Guid RecipientUserId, string? Title = null, string? Body = null,
    Guid? TemplateId = null, Dictionary<string, string>? Variables = null, Dictionary<string, string>? Data = null) : IRequest<SendUserPushResult>;
public sealed record SendUserPushResult(Guid BatchId, Guid SenderUserId, Guid RecipientUserId, int TotalDevices, IReadOnlyList<SendPushResult> Results)
{
    public int SentCount => Results.Count(x => x.Notification.Status == Domain.Entities.NotificationStatus.Sent);
    public int FailedCount => Results.Count(x => x.Notification.Status == Domain.Entities.NotificationStatus.Failed);
    public int UnknownCount => Results.Count(x => x.Notification.Status == Domain.Entities.NotificationStatus.Unknown);
}
