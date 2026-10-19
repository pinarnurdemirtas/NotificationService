namespace NotificationService.Application.Templates;
public sealed record TemplateResponse(Guid Id, string Code, string Title, string Body, bool IsActive, DateTime CreatedAtUtc);
