using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Devices;
using NotificationService.Application.Devices.Commands.RegisterDevice;
using NotificationService.Application.Devices.Commands.DeactivateDevice;
using NotificationService.Application.Devices.Queries.ListMyDevices;
namespace NotificationService.Api.Controllers;
[ApiController, Authorize]
[Route("api/devices")]
public sealed class DevicesController(ISender mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> Register(RegisterDeviceCommand command, CancellationToken ct) => Ok(await mediator.Send(command, ct));
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> List(CancellationToken ct) => Ok(await mediator.Send(new ListMyDevicesQuery(), ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    { await mediator.Send(new DeactivateDeviceCommand(id), ct); return NoContent(); }
}
