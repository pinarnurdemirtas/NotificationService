using MediatR;
namespace NotificationService.Application.Notifications.Queries.ListMyNotifications;
public sealed record ListMyNotificationsQuery(int Skip = 0, int Take = 20) : IRequest<IReadOnlyList<NotificationResponse>>;
