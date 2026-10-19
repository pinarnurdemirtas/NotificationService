using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Notifications.Queries.GetNotification;

public sealed class GetNotificationQueryHandler(INotificationRepository repository) : IRequestHandler<GetNotificationQuery, NotificationResponse>
{
    public async Task<NotificationResponse> Handle(GetNotificationQuery request, CancellationToken cancellationToken) =>
        NotificationMapping.Map(await repository.GetAsync(request.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Notification not found."));
}
