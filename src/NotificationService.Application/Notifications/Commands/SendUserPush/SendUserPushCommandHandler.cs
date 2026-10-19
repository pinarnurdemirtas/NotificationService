using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Application.Notifications.Commands.SendPushNotification;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Notifications.Commands.SendUserPush;
public sealed class SendUserPushCommandHandler(IUserRepository users, IUserDeviceRepository devices, ICurrentUser current, ISender mediator) : IRequestHandler<SendUserPushCommand, SendUserPushResult>
{
    public async Task<SendUserPushResult> Handle(SendUserPushCommand request, CancellationToken ct)
    {
        var senderId = current.GetRequiredUserId();
        if (await users.FindByIdAsync(senderId, ct) is null) throw new AuthenticationRequiredException("Sender no longer exists.");
        if (await users.FindByIdAsync(request.RecipientUserId, ct) is null) throw new ResourceNotFoundException("Recipient not found.");
        var targets = await devices.ListAsync(request.RecipientUserId, true, ct);
        if (targets.Count == 0) throw new ResourceConflictException("Recipient has no active device. They must sign in and enable notifications first.");
        var batchId = Guid.NewGuid();
        var results = new List<SendPushResult>();
        foreach (var device in targets)
        {
            if (ct.IsCancellationRequested && results.Count > 0) break;
            var command = new SendPushNotificationCommand(
                DeviceToken: device.TargetKind == PushTargetKind.FcmToken ? device.Token : null,
                InstallationId: device.TargetKind == PushTargetKind.FirebaseInstallationId ? device.Token : null,
                Title: request.Title, Body: request.Body, TemplateId: request.TemplateId, Variables: request.Variables, Data: request.Data,
                SenderUserId: senderId, RecipientUserId: request.RecipientUserId, UserDeviceId: device.Id, BatchId: batchId);
            var result = await mediator.Send(command, ct);
            results.Add(result);
            if (result.Notification.ErrorCode == "Unregistered")
            {
                device.Deactivate();
                using var saveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await devices.SaveChangesAsync(saveTimeout.Token);
            }
        }
        return new(batchId, senderId, request.RecipientUserId, targets.Count, results);
    }
}
