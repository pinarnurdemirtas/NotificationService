using NotificationService.Domain.Entities;
namespace NotificationService.Application.Templates;
internal static class TemplateMapping
{
    public static TemplateResponse Map(Template template) => new(template.Id, template.Code,
        template.Title, template.Body, template.IsActive, template.CreatedAtUtc);
}
