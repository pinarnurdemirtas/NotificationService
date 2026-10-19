using System.Text.Json;
using Microsoft.Extensions.FileProviders;
namespace NotificationService.Api.Development;

public static class DevelopmentPushTestExtensions
{
    public static WebApplication UseDevelopmentPushTest(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        var assets = Path.Combine(app.Environment.ContentRootPath, "Development", "PushTest");
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(assets),
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
        });
        app.MapGet("/development/firebase-config.js", (HttpContext context, IConfiguration configuration) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            // Deliberate allowlist: never serialize Firebase (server credentials) or ConnectionStrings.
            var web = configuration.GetSection("FirebaseWeb");
            var clientSettings = new
            {
                firebaseConfig = new
                {
                    apiKey = web["ApiKey"], authDomain = web["AuthDomain"], projectId = web["ProjectId"],
                    storageBucket = web["StorageBucket"], messagingSenderId = web["MessagingSenderId"], appId = web["AppId"]
                },
                vapidKey = web["VapidPublicKey"]
            };
            return Results.Text("self.firebaseWebSettings = " + JsonSerializer.Serialize(clientSettings) + ";",
                "application/javascript");
        }).ExcludeFromDescription();
        return app;
    }
}
