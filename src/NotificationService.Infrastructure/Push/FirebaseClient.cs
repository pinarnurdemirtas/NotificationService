using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using NotificationService.Application.Common;
namespace NotificationService.Infrastructure.Push;

// One named FirebaseApp per service provider, initialized only on first send.
public sealed class FirebaseClient(IOptions<FirebaseOptions> options) : IDisposable
{
    private readonly object gate = new();
    private FirebaseApp? app;
    public FirebaseMessaging GetMessaging()
    {
        var settings = options.Value;
        if (!settings.Enabled)
            throw new PushProviderException("FirebaseDisabled", "Enable Firebase in appsettings.Development.json.", PushFailureKind.Configuration);
        if (string.IsNullOrWhiteSpace(settings.ProjectId) || string.IsNullOrWhiteSpace(settings.CredentialsPath))
            throw new PushProviderException("FirebaseNotConfigured", "Firebase ProjectId and CredentialsPath are required.", PushFailureKind.Configuration);
        lock (gate)
        {
            if (app is not null) return FirebaseMessaging.GetMessaging(app);
            try
            {
                // Explicit service-account type avoids accepting arbitrary credential formats.
                var credential = CredentialFactory.FromFile<ServiceAccountCredential>(settings.CredentialsPath).ToGoogleCredential();
                app = FirebaseApp.Create(new AppOptions { Credential = credential, ProjectId = settings.ProjectId },
                    $"notification-service-{Guid.NewGuid():N}");
                return FirebaseMessaging.GetMessaging(app);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or Newtonsoft.Json.JsonException)
            {
                throw new PushProviderException("FirebaseCredentialsInvalid", "Firebase service-account file could not be loaded. Check the local configuration.", PushFailureKind.Configuration);
            }
        }
    }
    public void Dispose() { lock (gate) { app?.Delete(); app = null; } }
}
