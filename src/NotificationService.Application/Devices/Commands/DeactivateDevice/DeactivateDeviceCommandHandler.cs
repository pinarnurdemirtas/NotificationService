using MediatR;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Devices.Commands.DeactivateDevice;
public sealed class DeactivateDeviceCommandHandler(IUserDeviceRepository devices, ICurrentUser current) : IRequestHandler<DeactivateDeviceCommand>
{
    public async Task Handle(DeactivateDeviceCommand request, CancellationToken ct)
    {
        var device = await devices.FindAsync(request.Id, current.GetRequiredUserId(), ct)
            ?? throw new ResourceNotFoundException("Device not found.");
        device.Deactivate();
        await devices.SaveChangesAsync(ct);
    }
}
