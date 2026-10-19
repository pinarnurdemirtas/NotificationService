using MediatR;
using System.Security.Cryptography;
using System.Text;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
namespace NotificationService.Application.Devices.Commands.RegisterDevice;
public sealed class RegisterDeviceCommandHandler(IUserDeviceRepository devices, IUserRepository users, ICurrentUser current) : IRequestHandler<RegisterDeviceCommand, DeviceResponse>
{
    public async Task<DeviceResponse> Handle(RegisterDeviceCommand request, CancellationToken ct)
    {
        var userId = current.GetRequiredUserId();
        if (await users.FindByIdAsync(userId, ct) is null) throw new AuthenticationRequiredException("User no longer exists.");
        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 4096 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100 || !Enum.IsDefined(request.TargetKind))
            throw new RequestValidationException("Valid token, device name (1-100), and target kind are required.");
        var token = request.Token.Trim();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.TargetKind}:{token}")));
        var device = await devices.FindByHashAsync(hash, ct);
        if (device is not null && !device.IsActive && device.UserId != userId) device = null;
        if (device is not null)
        {
            if (device.IsActive && device.UserId != userId)
                throw new ResourceConflictException("This browser is registered to another user. Sign out there before switching accounts.");
            device.Register(userId, request.Name.Trim());
            await devices.SaveChangesAsync(ct);
        }
        else
        {
            device = UserDevice.Create(userId, token, hash, request.Name.Trim(), request.TargetKind);
            await devices.AddAsync(device, ct);
        }
        return DeviceResponse.From(device);
    }
}
