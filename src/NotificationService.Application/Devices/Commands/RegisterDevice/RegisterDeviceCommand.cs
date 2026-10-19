using MediatR;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Devices.Commands.RegisterDevice;
public sealed record RegisterDeviceCommand(string Token, string Name, PushTargetKind TargetKind = PushTargetKind.FcmToken) : IRequest<DeviceResponse>;
