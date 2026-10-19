using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Notifications.Queries.ListNotifications;

public sealed class ListNotificationsQueryHandler(INotificationRepository repository) : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    public async Task<IReadOnlyList<NotificationResponse>> Handle(ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (request.Skip < 0 || request.Take is < 1 or > 100)
            throw new RequestValidationException("Skip must be >= 0; take must be between 1 and 100.");
        return (await repository.ListAsync(request.Skip, request.Take, cancellationToken)).Select(NotificationMapping.Map).ToArray();
    }
}
