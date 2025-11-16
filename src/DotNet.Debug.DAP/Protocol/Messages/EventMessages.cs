using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol.Messages;

/// <summary>
/// Initialized event body (empty)
/// </summary>
public class InitializedEventBody
{
    // No properties - event has no body
}

/// <summary>
/// Stopped event body
/// </summary>
public class StoppedEventBody
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("threadId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThreadId { get; set; }

    [JsonPropertyName("preserveFocusHint")]
    public bool PreserveFocusHint { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("allThreadsStopped")]
    public bool AllThreadsStopped { get; set; }

    [JsonPropertyName("hitBreakpointIds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? HitBreakpointIds { get; set; }
}

/// <summary>
/// Continued event body
/// </summary>
public class ContinuedEventBody
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }

    [JsonPropertyName("allThreadsContinued")]
    public bool AllThreadsContinued { get; set; } = true;
}

/// <summary>
/// Exited event body
/// </summary>
public class ExitedEventBody
{
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }
}

/// <summary>
/// Terminated event body
/// </summary>
public class TerminatedEventBody
{
    [JsonPropertyName("restart")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Restart { get; set; }
}

/// <summary>
/// Thread event body
/// </summary>
public class ThreadEventBody
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }
}

/// <summary>
/// Output event body
/// </summary>
public class OutputEventBody
{
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Category { get; set; }

    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Group { get; set; }

    [JsonPropertyName("variablesReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VariablesReference { get; set; }

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Source? Source { get; set; }

    [JsonPropertyName("line")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Line { get; set; }

    [JsonPropertyName("column")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Column { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

/// <summary>
/// Breakpoint event body
/// </summary>
public class BreakpointEventBody
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("breakpoint")]
    public Breakpoint Breakpoint { get; set; } = new();
}

/// <summary>
/// Module event body
/// </summary>
public class ModuleEventBody
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("module")]
    public Module Module { get; set; } = new();
}

/// <summary>
/// Module information
/// </summary>
public class Module
{
    [JsonPropertyName("id")]
    public object Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    [JsonPropertyName("isOptimized")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOptimized { get; set; }

    [JsonPropertyName("isUserCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsUserCode { get; set; }

    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    [JsonPropertyName("symbolStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SymbolStatus { get; set; }

    [JsonPropertyName("symbolFilePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SymbolFilePath { get; set; }

    [JsonPropertyName("dateTimeStamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DateTimeStamp { get; set; }

    [JsonPropertyName("addressRange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AddressRange { get; set; }
}
