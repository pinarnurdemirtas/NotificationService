using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Notifications.Queries.GetMyNotification;
public sealed class GetMyNotificationQueryHandler(INotificationHistoryRepository history, ICurrentUser current) : IRequestHandler<GetMyNotificationQuery, NotificationResponse>
{
    public async Task<NotificationResponse> Handle(GetMyNotificationQuery request, CancellationToken ct) =>
        NotificationMapping.Map(await history.FindVisibleAsync(request.Id, current.GetRequiredUserId(), ct)
            ?? throw new ResourceNotFoundException("Notification not found."));
}
