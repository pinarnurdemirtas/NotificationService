using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NotificationService.Application.Users;
using NotificationService.Application.Users.Commands.Register;
using NotificationService.Application.Users.Commands.Login;
using NotificationService.Application.Users.Queries.GetCurrentUser;
namespace NotificationService.Api.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return Ok(await mediator.Send(command, ct)); }
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return Ok(await mediator.Send(command, ct)); }
    [HttpGet("me"), Authorize]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) => Ok(await mediator.Send(new GetCurrentUserQuery(), ct));
}
