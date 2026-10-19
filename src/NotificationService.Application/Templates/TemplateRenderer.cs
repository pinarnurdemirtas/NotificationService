using System.Text.RegularExpressions;
using NotificationService.Application.Common;
namespace NotificationService.Application.Templates;

public static partial class TemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}")]
    private static partial Regex Placeholder();

    public static string Render(string content, IReadOnlyDictionary<string, string> variables) =>
        Placeholder().Replace(content, match =>
            variables.TryGetValue(match.Groups[1].Value, out var value) && value is not null
                ? value
                : throw new RequestValidationException($"Missing template variable: {match.Groups[1].Value}"));
}
