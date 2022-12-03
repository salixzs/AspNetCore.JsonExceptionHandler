using System.Diagnostics;

namespace Salix.AspNetCore.JsonExceptionHandler;

/// <summary>
/// Structure to transfer validation errors to caller (used in list in <see cref="ApiError"/> object).
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public struct ApiDataValidationError : IEquatable<ApiDataValidationError>
{
    /// <summary>
    /// Name of the property which failed validation.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// What value was attempted to write to object's property.
    /// </summary>
    public object AttemptedValue { get; set; }

    /// <summary>
    /// Validation exception message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Implements the comparison operator ==.
    /// </summary>
    /// <param name="error1">ValidationError 1.</param>
    /// <param name="error2">ValidationError 2.</param>
    public static bool operator ==(ApiDataValidationError error1, ApiDataValidationError error2) => error1.Equals(error2);

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="error1">ValidationError 1.</param>
    /// <param name="error2">ValidationError 2.</param>
    public static bool operator !=(ApiDataValidationError error1, ApiDataValidationError error2) => !error1.Equals(error2);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        int hash = 13;
        hash = (hash * 7) + (string.IsNullOrEmpty(this.PropertyName) ? 0 : this.PropertyName.GetHashCode());
        return (hash * 7) + (this.AttemptedValue?.GetHashCode() ?? 0);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object" /> is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
    public override bool Equals(object? obj) => obj is ApiDataValidationError error && this.Equals(error);

    /// <summary>
    /// Determines whether the specified <see cref="ApiDataValidationError" /> is equal to this instance.
    /// </summary>
    /// <param name="other">The other ValidationError.</param>
    public bool Equals(ApiDataValidationError other) =>
        this.PropertyName == other.PropertyName && this.AttemptedValue.ToString() == other.AttemptedValue.ToString();

    /// <summary>
    /// Displays object main properties in Debug screen. (Only for development purposes).
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.PropertyName}: {this.Message}";
}
