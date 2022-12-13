using System.Diagnostics;

namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Inner exception - tree drilldown when multiple exception wrappers are used.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ApiErrorInner
{
    /// <summary>
    /// A short, human-readable summary of the problem type. It SHOULD NOT change from occurrence to occurrence of the problem,
    /// except for purposes of localization (e.g., using proactive content negotiation; see [RFC7231], Section 3.4).
    /// Message of an server error (Exception.Message) or general problem description.
    /// Should be in contract as per https://tools.ietf.org/html/rfc7807 .
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// If exception was thrown - here is exception type.
    /// </summary>
    public string ExceptionType { get; set; } = "Undetermined";

    /// <summary>
    /// Inner exception message, if such exists.
    /// </summary>
    public ApiErrorInner? InnerException { get; set; }

    /// <summary>
    /// Displays object main properties in Debug screen. (Only for development purposes).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.ExceptionType}: {this.Title}";
}
