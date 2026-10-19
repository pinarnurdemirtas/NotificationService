using MediatR;
namespace NotificationService.Application.Notifications.Queries.GetNotification;
public sealed record GetNotificationQuery(Guid Id) : IRequest<NotificationResponse>;
