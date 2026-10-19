using NotificationService.Domain.Entities;
namespace NotificationService.Application.Devices;
public sealed record DeviceResponse(Guid Id, string Name, bool IsActive, DateTime LastSeenAtUtc)
{
    public static DeviceResponse From(UserDevice device) => new(device.Id, device.Name, device.IsActive, device.LastSeenAtUtc);
}
