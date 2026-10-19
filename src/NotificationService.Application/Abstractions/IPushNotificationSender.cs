namespace NotificationService.Application.Abstractions;
// Firebase implementation will be added in the next step.
public interface IPushNotificationSender
{
    Task<string> SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default);
}
