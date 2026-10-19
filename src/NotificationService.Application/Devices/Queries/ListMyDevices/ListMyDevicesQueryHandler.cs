using MediatR;
using NotificationService.Application.Abstractions;
namespace NotificationService.Application.Devices.Queries.ListMyDevices;
public sealed class ListMyDevicesQueryHandler(IUserDeviceRepository devices, ICurrentUser current) : IRequestHandler<ListMyDevicesQuery, IReadOnlyList<DeviceResponse>>
{
    public async Task<IReadOnlyList<DeviceResponse>> Handle(ListMyDevicesQuery request, CancellationToken ct) =>
        (await devices.ListAsync(current.GetRequiredUserId(), false, ct)).Select(DeviceResponse.From).ToArray();
}
