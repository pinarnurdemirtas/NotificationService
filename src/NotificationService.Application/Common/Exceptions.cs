namespace NotificationService.Application.Common;

public sealed class RequestValidationException(string message) : Exception(message);
public sealed class ResourceNotFoundException(string message) : Exception(message);
public sealed class ResourceConflictException(string message) : Exception(message);
public enum PushFailureKind { Configuration, Rejected, Unavailable, Unknown }
public sealed class PushProviderException(string code, string message, PushFailureKind kind) : Exception(message)
{
    public string Code { get; } = code;
    public PushFailureKind Kind { get; } = kind;
}

public sealed class AuthenticationRequiredException(string message) : Exception(message);
