using MediatR;
namespace NotificationService.Application.Devices.Commands.DeactivateDevice;
public sealed record DeactivateDeviceCommand(Guid Id) : IRequest;
