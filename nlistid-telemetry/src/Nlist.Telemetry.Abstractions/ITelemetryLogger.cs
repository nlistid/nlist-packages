using System.Runtime.CompilerServices;

namespace Nlist.Telemetry.Abstractions;

public interface ITelemetryLogger<T>
{
    void LogInformation(
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    void LogWarning(
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0);

    void LogError(
        Exception? exception,
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0);
}
