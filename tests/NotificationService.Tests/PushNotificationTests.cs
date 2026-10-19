using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application;
using NotificationService.Application.Notifications.Commands.SendPushNotification;
using NotificationService.Application.Notifications.Queries.GetNotification;
using NotificationService.Application.Notifications.Queries.ListNotifications;
using NotificationService.Application.Templates.Commands.CreateTemplate;
using NotificationService.Application.Templates.Queries.GetTemplate;
using NotificationService.Application.Templates.Queries.ListTemplates;
using NotificationService.Application.Abstractions;
using NotificationService.Application.Common;
using NotificationService.Application.Notifications;
using NotificationService.Application.Templates;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Push;
using Microsoft.Extensions.Options;
using Xunit;

namespace NotificationService.Tests;

public sealed class PushNotificationTests
{
    private readonly FakeNotifications notifications = new();
    private readonly FakeTemplates templates = new();
    private readonly FakeSender sender = new();
    private SendPushNotificationCommandHandler Handler => new(notifications, templates, sender);
    private static SendPushNotificationCommand Valid => new("test-device-token", "Hello", "World");

    [Fact]
    public async Task SuccessPersistsBeforeSendingAndRecordsProviderId()
    {
        sender.BeforeSend = () => Assert.Single(notifications.Items);
        var result = await Handler.Handle(Valid, default);
        Assert.Equal(NotificationStatus.Sent, result.Notification.Status);
        Assert.Equal("provider-id", result.Notification.ProviderMessageId);
        Assert.NotNull(result.Notification.SentAtUtc);
        Assert.Equal(1, notifications.Saves);
    }

    [Theory]
    [InlineData(PushFailureKind.Configuration, NotificationStatus.Failed)]
    [InlineData(PushFailureKind.Rejected, NotificationStatus.Failed)]
    [InlineData(PushFailureKind.Unavailable, NotificationStatus.Failed)]
    [InlineData(PushFailureKind.Unknown, NotificationStatus.Unknown)]
    public async Task ProviderFailureIsSaved(PushFailureKind kind, NotificationStatus status)
    {
        sender.Failure = new PushProviderException("TestFailure", "Safe error", kind);
        var result = await Handler.Handle(Valid, default);
        Assert.Equal(kind, result.FailureKind);
        Assert.Equal(status, result.Notification.Status);
        Assert.Equal("TestFailure", result.Notification.ErrorCode);
        Assert.Null(result.Notification.SentAtUtc);
        Assert.Equal(1, notifications.Saves);
    }

    [Fact]
    public async Task CancellationStillPersistsUnknownOutcomeWithIndependentToken()
    {
        using var cts = new CancellationTokenSource();
        sender.BeforeSend = cts.Cancel;
        sender.Failure = new OperationCanceledException();
        var result = await Handler.Handle(Valid, cts.Token);
        Assert.Equal(NotificationStatus.Unknown, result.Notification.Status);
        Assert.False(notifications.SaveTokenCancelled);
    }

    [Fact]
    public async Task DatabaseFailureNeverCallsFirebase()
    {
        notifications.FailAdd = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler.Handle(Valid, default));
        Assert.Null(sender.LastMessage);
    }

    [Fact]
    public async Task TemplateVariablesAndCustomDataAreResolvedAndStored()
    {
        templates.Template = new Template { Title = "Hello {{name}}", Body = "Order {{order}}" };
        var request = new SendPushNotificationCommand("token", TemplateId: templates.Template.Id,
            Variables: new() { ["name"] = "Pınar", ["order"] = "42" }, Data: new() { ["screen"] = "orders" });
        var result = await Handler.Handle(request, default);
        Assert.Equal("Hello Pınar", sender.LastMessage!.Title);
        Assert.Equal("Order 42", result.Notification.Body);
        Assert.Equal("orders", result.Notification.Data["screen"]);
        Assert.Equal(templates.Template.Id, result.Notification.TemplateId);
    }

    [Fact]
    public async Task MissingVariableDoesNotPersistOrSend()
    {
        templates.Template = new Template { Title = "Hello {{name}}", Body = "Body" };
        await Assert.ThrowsAsync<RequestValidationException>(() => Handler.Handle(new("token", TemplateId: templates.Template.Id), default));
        Assert.Empty(notifications.Items);
        Assert.Null(sender.LastMessage);
    }

    [Fact]
    public async Task InactiveTemplateIsRejected()
    {
        templates.Template = new Template { Title = "Hello", Body = "Body", IsActive = false };
        await Assert.ThrowsAsync<RequestValidationException>(() => Handler.Handle(new("token", TemplateId: templates.Template.Id), default));
    }

    [Fact]
    public async Task MissingTemplateIsNotFound() =>
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => Handler.Handle(new("token", TemplateId: Guid.NewGuid()), default));

    [Fact]
    public async Task InstallationIdTargetIsPassedToProvider()
    {
        var result = await Handler.Handle(new(Title: "Hello", Body: "Body", InstallationId: "installation-id"), default);
        Assert.Equal(PushTargetKind.FirebaseInstallationId, sender.LastMessage!.TargetKind);
        Assert.Equal("installation-id", sender.LastMessage.Target);
        Assert.Equal(PushTargetKind.FirebaseInstallationId, result.Notification.TargetKind);
    }

    [Fact]
    public async Task BothTargetsAreRejected() =>
        await Assert.ThrowsAsync<RequestValidationException>(() => Handler.Handle(Valid with { InstallationId = "fid" }, default));

    [Fact]
    public async Task BlankTargetIsRejected() =>
        await Assert.ThrowsAsync<RequestValidationException>(() => Handler.Handle(Valid with { DeviceToken = " " }, default));

    [Fact]
    public async Task ReservedDataIsRejected() =>
        await Assert.ThrowsAsync<RequestValidationException>(() => Handler.Handle(Valid with { Data = new() { ["google.test"] = "x" } }, default));

    [Fact]
    public async Task Utf8PayloadSizeIsValidatedBeforeSend() =>
        await Assert.ThrowsAsync<RequestValidationException>(() => Handler.Handle(Valid with { Body = new string('界', 2000) }, default));

    [Fact]
    public async Task MissingNotificationIsNotFound() =>
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => new GetNotificationQueryHandler(notifications).Handle(new(Guid.NewGuid()), default));

    [Fact]
    public async Task PaginationIsBounded() =>
        await Assert.ThrowsAsync<RequestValidationException>(() => new ListNotificationsQueryHandler(notifications).Handle(new(0, 101), default));

    [Fact]
    public async Task DuplicateTemplateCodeIsRejected()
    {
        templates.Template = new Template { Code = "WELCOME" };
        await Assert.ThrowsAsync<ResourceConflictException>(() => new CreateTemplateCommandHandler(templates).Handle(new("welcome", "Hello", "Body"), default));
    }

    [Fact]
    public void CompletedNotificationCannotBeSentAgain()
    {
        var notification = Notification.CreatePush("token", "Title", "Body", null, null);
        notification.MarkSent("id");
        Assert.Throws<InvalidOperationException>(() => notification.MarkSent("other-id"));
    }

    [Fact]
    public async Task DisabledFirebaseReturnsConfigurationErrorWithoutNetwork()
    {
        using var client = new FirebaseClient(Options.Create(new FirebaseOptions { Enabled = false }));
        var provider = new FirebasePushNotificationSender(client, Options.Create(new FirebaseOptions()));
        var error = await Assert.ThrowsAsync<PushProviderException>(() => provider.SendAsync(new("token", "Title", "Body", new Dictionary<string, string>())));
        Assert.Equal("FirebaseDisabled", error.Code);
    }

    [Fact]
    public async Task MediatorDiscoversAndDispatchesAllCommandsAndQueries()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<INotificationRepository>(notifications);
        services.AddSingleton<ITemplateRepository>(templates);
        services.AddSingleton<IPushNotificationSender>(sender);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
        var template = await mediator.Send(new CreateTemplateCommand("MEDIATOR", "Hello", "Body"));
        Assert.Equal(template.Id, (await mediator.Send(new GetTemplateQuery(template.Id))).Id);
        Assert.Single(await mediator.Send(new ListTemplatesQuery()));
        var sent = await mediator.Send(Valid);
        Assert.Equal(NotificationStatus.Sent, sent.Notification.Status);
        Assert.Equal(sent.Notification.Id, (await mediator.Send(new GetNotificationQuery(sent.Notification.Id))).Id);
        Assert.Single(await mediator.Send(new ListNotificationsQuery()));
    }

    private sealed class FakeSender : IPushNotificationSender
    {
        public Action? BeforeSend { get; set; }
        public Exception? Failure { get; set; }
        public PushMessage? LastMessage { get; private set; }
        public Task<string> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            BeforeSend?.Invoke(); LastMessage = message;
            return Failure is null ? Task.FromResult("provider-id") : Task.FromException<string>(Failure);
        }
    }

    private sealed class FakeNotifications : INotificationRepository
    {
        public List<Notification> Items { get; } = [];
        public bool FailAdd { get; set; }
        public int Saves { get; private set; }
        public bool SaveTokenCancelled { get; private set; }
        public Task AddAsync(Notification notification, CancellationToken ct)
        {
            if (FailAdd) throw new InvalidOperationException("Database unavailable");
            Items.Add(notification); return Task.CompletedTask;
        }
        public Task<Notification?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Notification>> ListAsync(int skip, int take, CancellationToken ct) => Task.FromResult<IReadOnlyList<Notification>>(Items.Skip(skip).Take(take).ToArray());
        public Task SaveChangesAsync(CancellationToken ct) { Saves++; SaveTokenCancelled = ct.IsCancellationRequested; return Task.CompletedTask; }
    }

    private sealed class FakeTemplates : ITemplateRepository
    {
        public Template? Template { get; set; }
        public Task<Template?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(Template?.Id == id ? Template : null);
        public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => Task.FromResult(Template?.Code == code);
        public Task<IReadOnlyList<Template>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Template>>(Template is null ? [] : [Template]);
        public Task AddAsync(Template template, CancellationToken ct) { Template = template; return Task.CompletedTask; }
    }
}
