using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol.Messages;

/// <summary>
/// Arguments for Continue request
/// </summary>
public class ContinueArguments
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }
}

/// <summary>
/// Response body for Continue request
/// </summary>
public class ContinueResponseBody
{
    [JsonPropertyName("allThreadsContinued")]
    public bool AllThreadsContinued { get; set; } = true;
}

/// <summary>
/// Arguments for Next (step over) request
/// </summary>
public class NextArguments
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }

    [JsonPropertyName("granularity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Granularity { get; set; }
}

/// <summary>
/// Arguments for StepIn request
/// </summary>
public class StepInArguments
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }

    [JsonPropertyName("granularity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Granularity { get; set; }
}

/// <summary>
/// Arguments for StepOut request
/// </summary>
public class StepOutArguments
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }

    [JsonPropertyName("granularity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Granularity { get; set; }
}

/// <summary>
/// Arguments for Pause request
/// </summary>
public class PauseArguments
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }
}

/// <summary>
/// Arguments for StackTrace request
/// </summary>
public class StackTraceArguments
{
    [JsonPropertyName("threadId")]
    public int ThreadId { get; set; }

    [JsonPropertyName("startFrame")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StartFrame { get; set; }

    [JsonPropertyName("levels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Levels { get; set; }

    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StackFrameFormat? Format { get; set; }
}

/// <summary>
/// Stack frame format options
/// </summary>
public class StackFrameFormat
{
    [JsonPropertyName("parameters")]
    public bool Parameters { get; set; } = true;

    [JsonPropertyName("parameterTypes")]
    public bool ParameterTypes { get; set; } = true;

    [JsonPropertyName("parameterNames")]
    public bool ParameterNames { get; set; } = true;

    [JsonPropertyName("parameterValues")]
    public bool ParameterValues { get; set; } = true;

    [JsonPropertyName("line")]
    public bool Line { get; set; } = true;

    [JsonPropertyName("module")]
    public bool Module { get; set; } = true;

    [JsonPropertyName("includeAll")]
    public bool IncludeAll { get; set; } = false;
}

/// <summary>
/// Response body for StackTrace request
/// </summary>
public class StackTraceResponseBody
{
    [JsonPropertyName("stackFrames")]
    public StackFrame[] StackFrames { get; set; } = Array.Empty<StackFrame>();

    [JsonPropertyName("totalFrames")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalFrames { get; set; }
}

/// <summary>
/// Stack frame information
/// </summary>
public class StackFrame
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Source? Source { get; set; }

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("column")]
    public int Column { get; set; }

    [JsonPropertyName("endLine")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndLine { get; set; }

    [JsonPropertyName("endColumn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndColumn { get; set; }

    [JsonPropertyName("moduleId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ModuleId { get; set; }

    [JsonPropertyName("presentationHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PresentationHint { get; set; }
}

/// <summary>
/// Arguments for Threads request
/// </summary>
public class ThreadsArguments
{
    // No arguments for Threads request
}

/// <summary>
/// Response body for Threads request
/// </summary>
public class ThreadsResponseBody
{
    [JsonPropertyName("threads")]
    public Thread[] Threads { get; set; } = Array.Empty<Thread>();
}

/// <summary>
/// Thread information
/// </summary>
public class Thread
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
