using NotificationService.Domain.Entities;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Infrastructure.Push;

public sealed class FirebasePushNotificationSender(FirebaseClient client, IOptions<FirebaseOptions> options) : IPushNotificationSender
{
    public async Task<string> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
    {
        var messaging = client.GetMessaging();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            var firebaseMessage = new Message
            {
                Notification = new FirebaseAdmin.Messaging.Notification { Title = message.Title, Body = message.Body },
                Data = message.Data
            };
            if (message.TargetKind == PushTargetKind.FirebaseInstallationId) firebaseMessage.Fid = message.Target;
            else
            {
                // Compatibility for clients that still expose FCM registration tokens.
#pragma warning disable CS0618
                firebaseMessage.Token = message.Target;
#pragma warning restore CS0618
            }
            return await messaging.SendAsync(firebaseMessage, timeout.Token);
        }
        catch (FirebaseMessagingException ex)
        {
            var code = ex.MessagingErrorCode?.ToString() ?? ex.ErrorCode.ToString();
            var kind = ex.MessagingErrorCode switch
            {
                MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument or MessagingErrorCode.SenderIdMismatch => PushFailureKind.Rejected,
                MessagingErrorCode.ThirdPartyAuthError => PushFailureKind.Configuration,
                _ => PushFailureKind.Unavailable
            };
            throw new PushProviderException(code, kind == PushFailureKind.Rejected
                ? "Firebase rejected the push. Check the token, project, and payload."
                : "Firebase could not accept the push. Check provider configuration or availability.", kind);
        }
        catch (FirebaseException)
        { throw new PushProviderException("FirebaseError", "Firebase authentication or provider request failed.", PushFailureKind.Unavailable); }
        catch (HttpRequestException)
        { throw new PushProviderException("NetworkError", "Network interrupted; delivery outcome is unknown.", PushFailureKind.Unknown); }
        catch (ArgumentException)
        { throw new PushProviderException("InvalidPayload", "Firebase payload is invalid.", PushFailureKind.Rejected); }
    }
}
