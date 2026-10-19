using MediatR;
namespace NotificationService.Application.Notifications.Queries.ListNotifications;
public sealed record ListNotificationsQuery(int Skip = 0, int Take = 20) : IRequest<IReadOnlyList<NotificationResponse>>;
