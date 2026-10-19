using MediatR;
using System.Text.Json;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Application.Templates;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Notifications.Commands.SendPushNotification;

public sealed class SendPushNotificationCommandHandler(
    INotificationRepository notifications,
    ITemplateRepository templates,
    IPushNotificationSender sender) : IRequestHandler<SendPushNotificationCommand, SendPushResult>
{
    public async Task<SendPushResult> Handle(SendPushNotificationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceToken) == string.IsNullOrWhiteSpace(request.InstallationId))
            throw new RequestValidationException("Provide exactly one of DeviceToken or InstallationId.");
        var target = (!string.IsNullOrWhiteSpace(request.DeviceToken) ? request.DeviceToken : request.InstallationId)!.Trim();
        var targetKind = string.IsNullOrWhiteSpace(request.DeviceToken) ? PushTargetKind.FirebaseInstallationId : PushTargetKind.FcmToken;
        if (target.Length > 4096) throw new RequestValidationException("Push target must not exceed 4096 characters.");
        string title, body;
        if (request.TemplateId.HasValue)
        {
            if (request.Title is not null || request.Body is not null)
                throw new RequestValidationException("Use either TemplateId or Title/Body.");
            var template = await templates.GetAsync(request.TemplateId.Value, cancellationToken)
                ?? throw new ResourceNotFoundException("Template not found.");
            if (!template.IsActive) throw new RequestValidationException("Template is inactive.");
            var variables = request.Variables ?? new Dictionary<string, string>();
            title = TemplateRenderer.Render(template.Title, variables);
            body = TemplateRenderer.Render(template.Body, variables);
        }
        else
        {
            if (request.Variables is { Count: > 0 })
                throw new RequestValidationException("Variables require a TemplateId.");
            title = request.Title?.Trim() ?? string.Empty;
            body = request.Body?.Trim() ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 || string.IsNullOrWhiteSpace(body) || body.Length > 3000)
            throw new RequestValidationException("Title (1-200) and body (1-3000) are required after rendering.");
        var data = request.Data ?? new Dictionary<string, string>();
        foreach (var (key, value) in data)
            if (string.IsNullOrWhiteSpace(key) || value is null || key == "from" || key == "message_type" ||
                key.StartsWith("google.", StringComparison.Ordinal) || key.StartsWith("gcm.", StringComparison.Ordinal))
                throw new RequestValidationException("Data contains an empty or reserved Firebase key, or a null value.");
        if (JsonSerializer.SerializeToUtf8Bytes(new { notification = new { title, body }, data }).Length > 4096)
            throw new RequestValidationException("Push payload must not exceed 4096 UTF-8 bytes.");

        var notification = Notification.CreatePush(target, title, body, request.TemplateId,
            data.Count == 0 ? null : JsonSerializer.Serialize(data), targetKind);
        if (request.SenderUserId is { } senderId && request.RecipientUserId is { } recipientId && request.UserDeviceId is { } deviceId && request.BatchId is { } batchId)
            notification.AssociateUsers(senderId, recipientId, deviceId, batchId);
        // Persist before calling FCM: a database failure must never send an untracked push.
        await notifications.AddAsync(notification, cancellationToken);
        PushFailureKind? failureKind = null;
        try
        {
            var messageId = await sender.SendAsync(new PushMessage(notification.Recipient, title, body, data, targetKind), cancellationToken);
            notification.MarkSent(messageId);
        }
        catch (PushProviderException ex)
        {
            notification.MarkFailed(ex.Code, ex.Message, ex.Kind == PushFailureKind.Unknown);
            failureKind = ex.Kind;
        }
        catch (OperationCanceledException)
        {
            // Cancellation cannot prove whether the remote provider already accepted the push.
            notification.MarkFailed("OutcomeUnknown", "Send was interrupted; delivery outcome is unknown.", true);
            failureKind = PushFailureKind.Unknown;
        }
        // A disconnected HTTP caller must not prevent recording the provider result.
        using var saveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await notifications.SaveChangesAsync(saveTimeout.Token);
        return new SendPushResult(NotificationMapping.Map(notification), failureKind);
    }
}
