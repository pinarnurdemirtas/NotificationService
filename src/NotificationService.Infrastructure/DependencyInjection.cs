using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Persistence;
namespace NotificationService.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
