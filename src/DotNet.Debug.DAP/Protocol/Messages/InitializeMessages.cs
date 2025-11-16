using System.Text.Json.Serialization;

namespace DotNet.Debug.DAP.Protocol.Messages;

/// <summary>
/// Arguments for Initialize request
/// </summary>
public class InitializeRequestArguments
{
    [JsonPropertyName("clientID")]
    public string ClientID { get; set; } = "dotnet-debug-mcp";

    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = ".NET Debug MCP";

    [JsonPropertyName("adapterID")]
    public string AdapterID { get; set; } = "coreclr";

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en-US";

    [JsonPropertyName("linesStartAt1")]
    public bool LinesStartAt1 { get; set; } = true;

    [JsonPropertyName("columnsStartAt1")]
    public bool ColumnsStartAt1 { get; set; } = true;

    [JsonPropertyName("pathFormat")]
    public string PathFormat { get; set; } = "path";

    [JsonPropertyName("supportsVariableType")]
    public bool SupportsVariableType { get; set; } = true;

    [JsonPropertyName("supportsVariablePaging")]
    public bool SupportsVariablePaging { get; set; } = false;

    [JsonPropertyName("supportsRunInTerminalRequest")]
    public bool SupportsRunInTerminalRequest { get; set; } = false;

    [JsonPropertyName("supportsMemoryReferences")]
    public bool SupportsMemoryReferences { get; set; } = false;

    [JsonPropertyName("supportsProgressReporting")]
    public bool SupportsProgressReporting { get; set; } = false;

    [JsonPropertyName("supportsInvalidatedEvent")]
    public bool SupportsInvalidatedEvent { get; set; } = false;
}

/// <summary>
/// Response body for Initialize request
/// </summary>
public class Capabilities
{
    [JsonPropertyName("supportsConfigurationDoneRequest")]
    public bool SupportsConfigurationDoneRequest { get; set; }

    [JsonPropertyName("supportsFunctionBreakpoints")]
    public bool SupportsFunctionBreakpoints { get; set; }

    [JsonPropertyName("supportsConditionalBreakpoints")]
    public bool SupportsConditionalBreakpoints { get; set; }

    [JsonPropertyName("supportsHitConditionalBreakpoints")]
    public bool SupportsHitConditionalBreakpoints { get; set; }

    [JsonPropertyName("supportsEvaluateForHovers")]
    public bool SupportsEvaluateForHovers { get; set; }

    [JsonPropertyName("supportsStepBack")]
    public bool SupportsStepBack { get; set; }

    [JsonPropertyName("supportsSetVariable")]
    public bool SupportsSetVariable { get; set; }

    [JsonPropertyName("supportsRestartFrame")]
    public bool SupportsRestartFrame { get; set; }

    [JsonPropertyName("supportsGotoTargetsRequest")]
    public bool SupportsGotoTargetsRequest { get; set; }

    [JsonPropertyName("supportsStepInTargetsRequest")]
    public bool SupportsStepInTargetsRequest { get; set; }

    [JsonPropertyName("supportsCompletionsRequest")]
    public bool SupportsCompletionsRequest { get; set; }

    [JsonPropertyName("supportsModulesRequest")]
    public bool SupportsModulesRequest { get; set; }

    [JsonPropertyName("supportsRestartRequest")]
    public bool SupportsRestartRequest { get; set; }

    [JsonPropertyName("supportsExceptionOptions")]
    public bool SupportsExceptionOptions { get; set; }

    [JsonPropertyName("supportsValueFormattingOptions")]
    public bool SupportsValueFormattingOptions { get; set; }

    [JsonPropertyName("supportsExceptionInfoRequest")]
    public bool SupportsExceptionInfoRequest { get; set; }

    [JsonPropertyName("supportTerminateDebuggee")]
    public bool SupportTerminateDebuggee { get; set; }

    [JsonPropertyName("supportsDelayedStackTraceLoading")]
    public bool SupportsDelayedStackTraceLoading { get; set; }

    [JsonPropertyName("supportsLoadedSourcesRequest")]
    public bool SupportsLoadedSourcesRequest { get; set; }

    [JsonPropertyName("supportsLogPoints")]
    public bool SupportsLogPoints { get; set; }

    [JsonPropertyName("supportsTerminateThreadsRequest")]
    public bool SupportsTerminateThreadsRequest { get; set; }

    [JsonPropertyName("supportsSetExpression")]
    public bool SupportsSetExpression { get; set; }

    [JsonPropertyName("supportsTerminateRequest")]
    public bool SupportsTerminateRequest { get; set; }

    [JsonPropertyName("supportsDataBreakpoints")]
    public bool SupportsDataBreakpoints { get; set; }

    [JsonPropertyName("supportsReadMemoryRequest")]
    public bool SupportsReadMemoryRequest { get; set; }

    [JsonPropertyName("supportsDisassembleRequest")]
    public bool SupportsDisassembleRequest { get; set; }

    [JsonPropertyName("supportsCancelRequest")]
    public bool SupportsCancelRequest { get; set; }

    [JsonPropertyName("supportsBreakpointLocationsRequest")]
    public bool SupportsBreakpointLocationsRequest { get; set; }

    [JsonPropertyName("exceptionBreakpointFilters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExceptionBreakpointFilter[]? ExceptionBreakpointFilters { get; set; }
}

/// <summary>
/// Exception breakpoint filter
/// </summary>
public class ExceptionBreakpointFilter
{
    [JsonPropertyName("filter")]
    public string Filter { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("default")]
    public bool Default { get; set; }
}
