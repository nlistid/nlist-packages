using Microsoft.Extensions.Logging;

namespace NlistId.Telemetry.Test.Fakes;

/// <summary>One call to <see cref="RecordingLogger{T}"/>, with the message its formatter returned.</summary>
public sealed record LoggedEntry(
    LogLevel Level,
    IReadOnlyList<KeyValuePair<string, object?>>? State,
    Exception? Exception,
    string Message);
