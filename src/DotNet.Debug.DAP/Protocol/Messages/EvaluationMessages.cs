using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol.Messages;

/// <summary>
/// Arguments for Evaluate request
/// </summary>
public class EvaluateArguments
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("frameId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FrameId { get; set; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Context { get; set; }

    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ValueFormat? Format { get; set; }
}

/// <summary>
/// Response body for Evaluate request
/// </summary>
public class EvaluateResponseBody
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    [JsonPropertyName("presentationHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VariablePresentationHint? PresentationHint { get; set; }

    [JsonPropertyName("variablesReference")]
    public int VariablesReference { get; set; }

    [JsonPropertyName("namedVariables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NamedVariables { get; set; }

    [JsonPropertyName("indexedVariables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? IndexedVariables { get; set; }
}

/// <summary>
/// Arguments for Scopes request
/// </summary>
public class ScopesArguments
{
    [JsonPropertyName("frameId")]
    public int FrameId { get; set; }
}

/// <summary>
/// Response body for Scopes request
/// </summary>
public class ScopesResponseBody
{
    [JsonPropertyName("scopes")]
    public Scope[] Scopes { get; set; } = Array.Empty<Scope>();
}

/// <summary>
/// Scope information
/// </summary>
public class Scope
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("presentationHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PresentationHint { get; set; }

    [JsonPropertyName("variablesReference")]
    public int VariablesReference { get; set; }

    [JsonPropertyName("namedVariables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NamedVariables { get; set; }

    [JsonPropertyName("indexedVariables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? IndexedVariables { get; set; }

    [JsonPropertyName("expensive")]
    public bool Expensive { get; set; }

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Source? Source { get; set; }

    [JsonPropertyName("line")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Line { get; set; }

    [JsonPropertyName("column")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Column { get; set; }

    [JsonPropertyName("endLine")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndLine { get; set; }

    [JsonPropertyName("endColumn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? EndColumn { get; set; }
}

/// <summary>
/// Arguments for Variables request
/// </summary>
public class VariablesArguments
{
    [JsonPropertyName("variablesReference")]
    public int VariablesReference { get; set; }

    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Filter { get; set; }

    [JsonPropertyName("start")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Start { get; set; }

    [JsonPropertyName("count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Count { get; set; }

    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ValueFormat? Format { get; set; }
}

/// <summary>
/// Response body for Variables request
/// </summary>
public class VariablesResponseBody
{
    [JsonPropertyName("variables")]
    public Variable[] Variables { get; set; } = Array.Empty<Variable>();
}

/// <summary>
/// Variable information
/// </summary>
public class Variable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    [JsonPropertyName("presentationHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VariablePresentationHint? PresentationHint { get; set; }

    [JsonPropertyName("evaluateName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EvaluateName { get; set; }

    [JsonPropertyName("variablesReference")]
    public int VariablesReference { get; set; }

    [JsonPropertyName("namedVariables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? NamedVariables { get; set; }

    [JsonPropertyName("indexedVariables")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? IndexedVariables { get; set; }

    [JsonPropertyName("memoryReference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MemoryReference { get; set; }
}

/// <summary>
/// Value format options
/// </summary>
public class ValueFormat
{
    [JsonPropertyName("hex")]
    public bool Hex { get; set; }
}

/// <summary>
/// Variable presentation hint
/// </summary>
public class VariablePresentationHint
{
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; set; }

    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Attributes { get; set; }

    [JsonPropertyName("visibility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Visibility { get; set; }
}
