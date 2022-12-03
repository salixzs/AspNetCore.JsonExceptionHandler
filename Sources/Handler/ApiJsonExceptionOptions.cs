namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Options for Error Handler Middleware.
/// </summary>
public sealed class ApiJsonExceptionOptions
{
    /// <summary>
    /// True means to Shows stack trace records in error response.
    /// Usually value for this option should be taken from Api.Hosting environment IsDevelopment() method.
    /// Pass constant "true" to show stack trace always.
    /// Default: false.
    /// </summary>
    public bool ShowStackTrace { get; set; }

    /// <summary>
    /// List of path names or file names to filter out from StackTrace frame list.
    /// </summary>
    /// <example>
    /// options.OmitSources
    /// </example>
    public HashSet<string> OmitSources { get; set; } = new();
}
