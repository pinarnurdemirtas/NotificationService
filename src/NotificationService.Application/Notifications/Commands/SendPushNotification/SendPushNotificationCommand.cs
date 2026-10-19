using MediatR;
namespace NotificationService.Application.Notifications.Commands.SendPushNotification;

public sealed record SendPushNotificationCommand(
    string? DeviceToken = null,
    string? Title = null,
    string? Body = null,
    Guid? TemplateId = null,
    Dictionary<string, string>? Variables = null,
    Dictionary<string, string>? Data = null,
    string? InstallationId = null,
    Guid? SenderUserId = null, Guid? RecipientUserId = null, Guid? UserDeviceId = null, Guid? BatchId = null) : IRequest<SendPushResult>;
