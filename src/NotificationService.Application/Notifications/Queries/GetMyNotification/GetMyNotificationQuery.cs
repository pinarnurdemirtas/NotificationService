using MediatR;
namespace NotificationService.Application.Notifications.Queries.GetMyNotification;
public sealed record GetMyNotificationQuery(Guid Id) : IRequest<NotificationResponse>;
