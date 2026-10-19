using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Users;
using NotificationService.Application.Users.Queries.ListUsers;
namespace NotificationService.Api.Controllers;
[ApiController, Authorize]
[Route("api/users")]
public sealed class UsersController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken ct) => Ok(await mediator.Send(new ListUsersQuery(), ct));
}
