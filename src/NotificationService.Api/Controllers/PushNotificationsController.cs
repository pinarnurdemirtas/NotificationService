using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Common;
using NotificationService.Application.Notifications;
using NotificationService.Application.Notifications.Commands.SendUserPush;
using NotificationService.Application.Notifications.Queries.GetMyNotification;
using NotificationService.Application.Notifications.Queries.ListMyNotifications;
namespace NotificationService.Api.Controllers;
[ApiController, Authorize]
[Route("api/push-notifications")]
public sealed class PushNotificationsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SendUserPushResult>(200)]
    [ProducesResponseType<SendUserPushResult>(207)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<SendUserPushResult>(422)]
    [ProducesResponseType<SendUserPushResult>(503)]
    [ProducesResponseType<SendUserPushResult>(504)]
    public async Task<IActionResult> Send(SendUserPushCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        var completedAllDevices = result.Results.Count == result.TotalDevices;
        var statusCode = completedAllDevices && result.Results.All(x => x.FailureKind is null) ? 200
            : result.SentCount > 0 || !completedAllDevices ? 207
            : result.Results.Any(x => x.FailureKind == PushFailureKind.Unknown) ? 504
            : result.Results.Any(x => x.FailureKind is PushFailureKind.Configuration or PushFailureKind.Unavailable) ? 503 : 422;
        return StatusCode(statusCode, result);
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetMyNotificationQuery(id), ct));
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> List(CancellationToken ct, [FromQuery] int skip = 0, [FromQuery] int take = 20) =>
        Ok(await mediator.Send(new ListMyNotificationsQuery(skip, take), ct));
}
