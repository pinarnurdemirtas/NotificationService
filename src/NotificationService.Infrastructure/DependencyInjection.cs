using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Abstractions;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Repositories;
using NotificationService.Infrastructure.Push;
namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDb")
            ?? throw new InvalidOperationException("ConnectionStrings:NotificationDb is required.");
        services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();
        services.AddSingleton<IUserPasswordHasher, UserPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"))
            .Validate(x => x.SigningKey.Length >= 64 && !string.IsNullOrWhiteSpace(x.Issuer) && !string.IsNullOrWhiteSpace(x.Audience)
                && x.LifetimeMinutes is >= 5 and <= 120, "Configure Jwt issuer, audience, signing key (at least 64 characters), and lifetime (5-120 minutes).")
            .ValidateOnStart();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddOptions<FirebaseOptions>().Bind(configuration.GetSection(FirebaseOptions.SectionName))
            .Validate(x => x.TimeoutSeconds is >= 1 and <= 120, "Firebase TimeoutSeconds must be between 1 and 120.")
            .ValidateOnStart();
        services.AddSingleton<FirebaseClient>();
        services.AddSingleton<IPushNotificationSender, FirebasePushNotificationSender>();
        return services;
    }
}
