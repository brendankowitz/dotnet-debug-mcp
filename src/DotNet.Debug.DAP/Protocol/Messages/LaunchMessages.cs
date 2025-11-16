using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol.Messages;

/// <summary>
/// Arguments for Launch request
/// </summary>
public class LaunchRequestArguments
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "coreclr";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Debug .NET Application";

    [JsonPropertyName("program")]
    public string Program { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Args { get; set; }

    [JsonPropertyName("cwd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cwd { get; set; }

    [JsonPropertyName("env")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Env { get; set; }

    [JsonPropertyName("stopAtEntry")]
    public bool StopAtEntry { get; set; } = false;

    [JsonPropertyName("console")]
    public string Console { get; set; } = "internalConsole";

    [JsonPropertyName("__restart")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Restart { get; set; }
}

/// <summary>
/// Arguments for Attach request
/// </summary>
public class AttachRequestArguments
{
    [JsonPropertyName("processId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ProcessId { get; set; }

    [JsonPropertyName("processName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProcessName { get; set; }
}
