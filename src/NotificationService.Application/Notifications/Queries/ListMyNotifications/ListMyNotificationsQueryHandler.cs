using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Notifications.Queries.ListMyNotifications;
public sealed class ListMyNotificationsQueryHandler(INotificationHistoryRepository history, ICurrentUser current) : IRequestHandler<ListMyNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<IReadOnlyList<NotificationResponse>> Handle(ListMyNotificationsQuery request, CancellationToken ct)
    {
        if (request.Skip < 0 || request.Take is < 1 or > 100) throw new RequestValidationException("Invalid pagination.");
        return (await history.ListVisibleAsync(current.GetRequiredUserId(), request.Skip, request.Take, ct)).Select(NotificationMapping.Map).ToArray();
    }
}
