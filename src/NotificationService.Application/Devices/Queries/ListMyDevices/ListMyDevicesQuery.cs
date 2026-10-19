using MediatR;
namespace NotificationService.Application.Devices.Queries.ListMyDevices;
public sealed record ListMyDevicesQuery : IRequest<IReadOnlyList<DeviceResponse>>;
