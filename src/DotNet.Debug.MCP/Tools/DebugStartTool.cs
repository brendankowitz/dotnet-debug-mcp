using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to start a debugging session
/// </summary>
public class DebugStartTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public DebugStartTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_start",
        Description = "Start a debugging session for a .NET application. This launches the application under the debugger and prepares it for debugging operations like setting breakpoints and stepping through code.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["program"] = new()
                {
                    Type = "string",
                    Description = "Path to the .NET DLL to debug (e.g., /path/to/app.dll)"
                },
                ["args"] = new()
                {
                    Type = "array",
                    Description = "Command-line arguments to pass to the application",
                    Items = new McpProperty { Type = "string" }
                },
                ["cwd"] = new()
                {
                    Type = "string",
                    Description = "Working directory for the application (defaults to DLL directory)"
                },
                ["stopAtEntry"] = new()
                {
                    Type = "boolean",
                    Description = "Whether to stop at the entry point",
                    Default = false
                },
                ["sessionId"] = new()
                {
                    Type = "string",
                    Description = "Optional session ID (defaults to 'default')"
                }
            },
            Required = new[] { "program" }
        }
    };

    public async Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters);
            var args = JsonSerializer.Deserialize<DebugStartArgs>(json);

            if (args == null || string.IsNullOrEmpty(args.Program))
            {
                return ErrorResult("Missing required parameter: program");
            }

            // Validate program exists
            if (!File.Exists(args.Program))
            {
                return ErrorResult($"Program not found: {args.Program}");
            }

            var sessionId = args.SessionId ?? "default";

            var session = await _sessionManager.CreateSessionAsync(
                sessionId,
                args.Program,
                args.Args,
                args.Cwd,
                args.StopAtEntry ?? false,
                cancellationToken);

            var result = new
            {
                success = true,
                sessionId = session.SessionId,
                program = session.Program,
                message = $"Debug session '{sessionId}' started successfully",
                capabilities = new
                {
                    supportsConditionalBreakpoints = session.Capabilities?.SupportsConditionalBreakpoints ?? false,
                    supportsStepBack = session.Capabilities?.SupportsStepBack ?? false,
                    supportsEvaluateForHovers = session.Capabilities?.SupportsEvaluateForHovers ?? false
                }
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to start debug session: {ex.Message}");
        }
    }

    private static ToolResult SuccessResult(string text) => new()
    {
        Content = new[] { new ToolContent { Type = "text", Text = text } }
    };

    private static ToolResult ErrorResult(string message) => new()
    {
        Content = new[] { new ToolContent { Type = "text", Text = message } },
        IsError = true
    };
}

public class DebugStartArgs
{
    [JsonPropertyName("program")]
    public string Program { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public string[]? Args { get; set; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("stopAtEntry")]
    public bool? StopAtEntry { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
