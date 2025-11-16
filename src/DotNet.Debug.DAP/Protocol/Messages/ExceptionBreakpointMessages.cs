using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol.Messages;

/// <summary>
/// Arguments for SetExceptionBreakpoints request
/// </summary>
public class SetExceptionBreakpointsArguments
{
    [JsonPropertyName("filters")]
    public string[] Filters { get; set; } = Array.Empty<string>();

    [JsonPropertyName("exceptionOptions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExceptionOptions[]? ExceptionOptions { get; set; }
}

/// <summary>
/// Exception options for specific exception types
/// </summary>
public class ExceptionOptions
{
    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExceptionPathSegment[]? Path { get; set; }

    [JsonPropertyName("breakMode")]
    public string BreakMode { get; set; } = "always";
}

/// <summary>
/// Exception path segment for filtering
/// </summary>
public class ExceptionPathSegment
{
    [JsonPropertyName("names")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Names { get; set; }

    [JsonPropertyName("negate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Negate { get; set; }
}

/// <summary>
/// Response body for SetExceptionBreakpoints request
/// </summary>
public class SetExceptionBreakpointsResponseBody
{
    [JsonPropertyName("breakpoints")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Breakpoint[]? Breakpoints { get; set; }
}
