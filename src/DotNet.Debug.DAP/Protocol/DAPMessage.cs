using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol;

/// <summary>
/// Base class for all DAP protocol messages
/// </summary>
public abstract class DAPMessage
{
    /// <summary>
    /// Sequence number (sequential)
    /// </summary>
    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    /// <summary>
    /// Message type: "request", "response", or "event"
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// DAP Request message
/// </summary>
public class DAPRequest : DAPMessage
{
    public override string Type => "request";

    /// <summary>
    /// The command to execute
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Object containing command-specific arguments
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Arguments { get; set; }
}

/// <summary>
/// DAP Request with typed arguments
/// </summary>
public class DAPRequest<TArguments> : DAPMessage where TArguments : class
{
    public override string Type => "request";

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TArguments? Arguments { get; set; }
}

/// <summary>
/// DAP Response message
/// </summary>
public class DAPResponse : DAPMessage
{
    public override string Type => "response";

    /// <summary>
    /// Sequence number of the corresponding request
    /// </summary>
    [JsonPropertyName("request_seq")]
    public int RequestSeq { get; set; }

    /// <summary>
    /// Whether the request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The command that was requested
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Error message if success is false
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>
    /// Response body containing command-specific data
    /// </summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; }
}

/// <summary>
/// DAP Response with typed body
/// </summary>
public class DAPResponse<TBody> : DAPMessage where TBody : class
{
    public override string Type => "response";

    [JsonPropertyName("request_seq")]
    public int RequestSeq { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TBody? Body { get; set; }
}

/// <summary>
/// DAP Event message
/// </summary>
public class DAPEvent : DAPMessage
{
    public override string Type => "event";

    /// <summary>
    /// Type of event
    /// </summary>
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// Event-specific data
    /// </summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; set; }
}

/// <summary>
/// DAP Event with typed body
/// </summary>
public class DAPEvent<TBody> : DAPMessage where TBody : class
{
    public override string Type => "event";

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TBody? Body { get; set; }
}
