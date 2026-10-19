using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using NotificationService.Application;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Application.Devices.Commands.RegisterDevice;
using NotificationService.Application.Devices.Commands.DeactivateDevice;
using NotificationService.Application.Notifications.Commands.SendUserPush;
using NotificationService.Application.Notifications.Queries.GetMyNotification;
using NotificationService.Application.Notifications.Queries.ListMyNotifications;
using NotificationService.Application.Users.Commands.Register;
using NotificationService.Application.Users.Commands.Login;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Authentication;
using Xunit;

namespace NotificationService.Tests;
public sealed class UserFlowTests
{
    [Fact]
    public async Task RegistrationHashesPasswordAndLoginReturnsSignedToken()
    {
        using var f = new Fixture();
        var registered = await f.Mediator.Send(new RegisterCommand("Pinar", "Pınar", "long-password-123"));
        var user = Assert.Single(f.Users.Items);
        Assert.NotEqual("long-password-123", user.PasswordHash);
        var loggedIn = await f.Mediator.Send(new LoginCommand("PINAR", "long-password-123"));
        Assert.Equal(registered.User.Id, loggedIn.User.Id);
        new JwtSecurityTokenHandler().ValidateToken(loggedIn.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "test", ValidateAudience = true, ValidAudience = "test-web",
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Fixture.SigningKey)),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero
        }, out var token);
        Assert.Equal(user.Id.ToString(), ((JwtSecurityToken)token).Subject);
    }
    [Fact]
    public async Task InvalidPasswordAndDuplicateUsernameAreRejected()
    {
        using var f = new Fixture();
        await f.Mediator.Send(new RegisterCommand("pinar", "Pınar", "long-password-123"));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => f.Mediator.Send(new LoginCommand("pinar", "incorrect")));
        await Assert.ThrowsAsync<ResourceConflictException>(() => f.Mediator.Send(new RegisterCommand("PINAR", "Other", "long-password-123")));
    }
    [Fact]
    public async Task RegisterRejectsShortPassword()
    {
        using var f = new Fixture();
        await Assert.ThrowsAsync<RequestValidationException>(() => f.Mediator.Send(new RegisterCommand("pinar", "Pınar", "short")));
        Assert.Empty(f.Users.Items);
    }
    [Fact]
    public async Task AnonymousDeviceRegistrationIsRejected()
    {
        using var f = new Fixture();
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => f.Mediator.Send(new RegisterDeviceCommand("token", "Browser")));
    }
    [Fact]
    public async Task TokenRegistrationIsIdempotentAndCannotStealActiveDevice()
    {
        using var f = new Fixture();
        var pinar = f.AddUser("pinar"); var ayse = f.AddUser("ayse");
        f.Current.Id = ayse.Id;
        var device = await f.Mediator.Send(new RegisterDeviceCommand("ayse-token", "Edge"));
        var same = await f.Mediator.Send(new RegisterDeviceCommand("ayse-token", "Edge updated"));
        Assert.Equal(device.Id, same.Id); Assert.Single(f.Devices.Items);
        f.Current.Id = pinar.Id;
        await Assert.ThrowsAsync<ResourceConflictException>(() => f.Mediator.Send(new RegisterDeviceCommand("ayse-token", "Chrome")));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Mediator.Send(new DeactivateDeviceCommand(device.Id)));
    }
    [Fact]
    public async Task LoggedOutDeviceCanMoveWithoutChangingHistoricalDeviceOwnership()
    {
        using var f = new Fixture();
        var pinar = f.AddUser("pinar"); var ayse = f.AddUser("ayse");
        f.Current.Id = ayse.Id;
        var old = await f.Mediator.Send(new RegisterDeviceCommand("shared-browser-token", "Edge"));
        await f.Mediator.Send(new DeactivateDeviceCommand(old.Id));
        f.Current.Id = pinar.Id;
        var next = await f.Mediator.Send(new RegisterDeviceCommand("shared-browser-token", "Edge"));
        Assert.NotEqual(old.Id, next.Id);
        Assert.Equal(ayse.Id, f.Devices.Items.Single(x => x.Id == old.Id).UserId);
        Assert.Single(f.Devices.Items, x => x.IsActive);
    }
    [Fact]
    public async Task PinarSendsToAllAyseDevicesAndRecordsAuthenticatedSender()
    {
        using var f = new Fixture();
        var pinar = f.AddUser("pinar"); var ayse = f.AddUser("ayse"); var other = f.AddUser("other");
        f.Current.Id = ayse.Id;
        await f.Mediator.Send(new RegisterDeviceCommand("ayse-edge", "Edge"));
        await f.Mediator.Send(new RegisterDeviceCommand("ayse-mobile", "Mobile"));
        f.Current.Id = pinar.Id;
        await f.Mediator.Send(new RegisterDeviceCommand("pinar-chrome", "Chrome"));
        var result = await f.Mediator.Send(new SendUserPushCommand(ayse.Id, "Hello Ayşe", "From Pınar"));
        Assert.Equal(2, result.SentCount);
        Assert.Equal(2, result.TotalDevices);
        Assert.All(f.Notifications.Items, n => { Assert.Equal(pinar.Id, n.SenderUserId); Assert.Equal(ayse.Id, n.RecipientUserId); Assert.Equal(result.BatchId, n.BatchId); });
        Assert.DoesNotContain(f.Sender.Targets, x => x == "pinar-chrome");
        Assert.Equal(2, (await f.Mediator.Send(new ListMyNotificationsQuery())).Count);
        f.Current.Id = ayse.Id;
        Assert.Equal(2, (await f.Mediator.Send(new ListMyNotificationsQuery())).Count);
        f.Current.Id = other.Id;
        Assert.Empty(await f.Mediator.Send(new ListMyNotificationsQuery()));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Mediator.Send(new GetMyNotificationQuery(result.Results[0].Notification.Id)));
    }
    [Fact]
    public async Task UnregisteredTargetIsDeactivatedWhileOtherDeviceStillReceives()
    {
        using var f = new Fixture(); var pinar = f.AddUser("pinar"); var ayse = f.AddUser("ayse");
        f.Current.Id = ayse.Id;
        await f.Mediator.Send(new RegisterDeviceCommand("expired-token", "Old browser"));
        await f.Mediator.Send(new RegisterDeviceCommand("active-token", "New browser"));
        f.Sender.RejectTarget = "expired-token";
        f.Current.Id = pinar.Id;
        var result = await f.Mediator.Send(new SendUserPushCommand(ayse.Id, "Hello", "Body"));
        Assert.Equal(1, result.SentCount); Assert.Equal(1, result.FailedCount);
        Assert.False(f.Devices.Items.Single(x => x.Token == "expired-token").IsActive);
    }
    [Fact]
    public async Task MissingRecipientOrDeviceDoesNotSend()
    {
        using var f = new Fixture(); var pinar = f.AddUser("pinar"); var ayse = f.AddUser("ayse"); f.Current.Id = pinar.Id;
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Mediator.Send(new SendUserPushCommand(Guid.NewGuid(), "Hi", "Body")));
        await Assert.ThrowsAsync<ResourceConflictException>(() => f.Mediator.Send(new SendUserPushCommand(ayse.Id, "Hi", "Body")));
        Assert.Empty(f.Sender.Targets); Assert.Empty(f.Notifications.Items);
    }
    private sealed class Fixture : IDisposable
    {
        public const string SigningKey = "test-only-signing-key-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ-abcdefghijklmnopqrstuvwxyz";
        public Current Current { get; } = new();
        public Users Users { get; } = new();
        public Devices Devices { get; } = new();
        public Notifications Notifications { get; } = new();
        public Sender Sender { get; } = new();
        private readonly ServiceProvider provider;
        public ISender Mediator => provider.GetRequiredService<ISender>();
        public Fixture()
        {
            var services = new ServiceCollection().AddApplication();
            services.AddSingleton<ICurrentUser>(Current).AddSingleton<IUserRepository>(Users).AddSingleton<IUserDeviceRepository>(Devices);
            services.AddSingleton<INotificationRepository>(Notifications).AddSingleton<INotificationHistoryRepository>(Notifications);
            services.AddSingleton<ITemplateRepository, Templates>().AddSingleton<IPushNotificationSender>(Sender);
            services.AddSingleton<IUserPasswordHasher, UserPasswordHasher>();
            services.AddSingleton<IAccessTokenIssuer>(new JwtAccessTokenIssuer(Options.Create(new JwtOptions { Issuer = "test", Audience = "test-web", SigningKey = SigningKey })));
            provider = services.BuildServiceProvider();
        }
        public User AddUser(string name) { var user = User.Create(name, name); Users.Items.Add(user); return user; }
        public void Dispose() => provider.Dispose();
    }
    private sealed class Current : ICurrentUser
    { public Guid? Id { get; set; } public Guid GetRequiredUserId() => Id ?? throw new AuthenticationRequiredException("Sign in."); }
    private sealed class Users : IUserRepository
    {
        public List<User> Items { get; } = [];
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<User?> FindByUsernameAsync(string name, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.NormalizedUsername == name));
        public Task<IReadOnlyList<User>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<User>>(Items);
        public Task AddAsync(User user, CancellationToken ct) { Items.Add(user); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class Devices : IUserDeviceRepository
    {
        public List<UserDevice> Items { get; } = [];
        public Task<UserDevice?> FindByHashAsync(string hash, CancellationToken ct) => Task.FromResult(Items.Where(x => x.TokenHash == hash).OrderByDescending(x => x.IsActive).FirstOrDefault());
        public Task<UserDevice?> FindAsync(Guid id, Guid userId, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id && x.UserId == userId));
        public Task<IReadOnlyList<UserDevice>> ListAsync(Guid userId, bool active, CancellationToken ct) => Task.FromResult<IReadOnlyList<UserDevice>>(Items.Where(x => x.UserId == userId && (!active || x.IsActive)).ToArray());
        public Task AddAsync(UserDevice device, CancellationToken ct) { Items.Add(device); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class Notifications : INotificationRepository, INotificationHistoryRepository
    {
        public List<Notification> Items { get; } = [];
        public Task AddAsync(Notification notification, CancellationToken ct) { Items.Add(notification); return Task.CompletedTask; }
        public Task<Notification?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Notification>> ListAsync(int skip, int take, CancellationToken ct) => Task.FromResult<IReadOnlyList<Notification>>(Items.Skip(skip).Take(take).ToArray());
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<Notification?> FindVisibleAsync(Guid id, Guid userId, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id && (x.SenderUserId == userId || x.RecipientUserId == userId)));
        public Task<IReadOnlyList<Notification>> ListVisibleAsync(Guid userId, int skip, int take, CancellationToken ct) => Task.FromResult<IReadOnlyList<Notification>>(Items.Where(x => x.SenderUserId == userId || x.RecipientUserId == userId).Skip(skip).Take(take).ToArray());
    }
    private sealed class Sender : IPushNotificationSender
    {
        public List<string> Targets { get; } = [];
        public string? RejectTarget { get; set; }
        public Task<string> SendAsync(PushMessage message, CancellationToken ct = default)
        {
            Targets.Add(message.Target);
            return message.Target == RejectTarget ? Task.FromException<string>(new PushProviderException("Unregistered", "Expired token.", PushFailureKind.Rejected)) : Task.FromResult("provider-id");
        }
    }
    private sealed class Templates : ITemplateRepository
    {
        public Task<Template?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<Template?>(null);
        public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => Task.FromResult(false);
        public Task<IReadOnlyList<Template>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Template>>([]);
        public Task AddAsync(Template template, CancellationToken ct) => Task.CompletedTask;
    }
}
