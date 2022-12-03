namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Specifies the behavior of thrown exception/error.
/// </summary>
[Flags]
public enum ApiErrorBehavior
{
    /// <summary>
    /// Ignores error/exception completely. Does not log and do not throw back to client.<br/>
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Logs error with configured ILogger instance (your configured sinks).
    /// </summary>
    LogError = 1,

    /// <summary>
    /// Returns it as error to client (re-throws).
    /// </summary>
    RespondWithError = 2,

    /// <summary>
    /// Should be standard behavior - logs error and returns it to client as Json.
    /// </summary>
    LogAndThrowError = LogError | RespondWithError,
}
