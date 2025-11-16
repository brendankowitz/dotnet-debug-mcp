using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to get variables in current scope
/// </summary>
public class GetVariablesTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public GetVariablesTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_get_variables",
        Description = "Get all variables in the current scope (local variables, arguments, and members). This is essential for understanding program state when stopped at a breakpoint.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["scope"] = new()
                {
                    Type = "string",
                    Description = "Scope filter: 'locals', 'arguments', or 'all'",
                    Enum = new[] { "locals", "arguments", "all" },
                    Default = "all"
                },
                ["frameId"] = new()
                {
                    Type = "integer",
                    Description = "Stack frame ID (0 = current frame)",
                    Default = 0
                },
                ["sessionId"] = new()
                {
                    Type = "string",
                    Description = "Session ID (defaults to 'default')"
                }
            },
            Required = Array.Empty<string>()
        }
    };

    public async Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters ?? new { });
            var args = JsonSerializer.Deserialize<GetVariablesArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}");
            }

            if (!session.IsStopped)
            {
                return ErrorResult("Program is not stopped. Cannot get variables.");
            }

            var threadId = session.LastStopThreadId ?? 1;
            var frames = await session.Client.GetStackTraceAsync(threadId, cancellationToken);

            if (frames.Length == 0)
            {
                return ErrorResult("No stack frames available");
            }

            var frameId = args?.FrameId ?? 0;
            if (frameId >= frames.Length)
            {
                return ErrorResult($"Invalid frame ID {frameId}. Available: 0-{frames.Length - 1}");
            }

            // Get scopes for the frame
            var scopes = await session.Client.GetScopesAsync(frames[frameId].Id, cancellationToken);

            var scopeFilter = args?.Scope?.ToLowerInvariant() ?? "all";
            var allVariables = new List<object>();

            foreach (var scope in scopes)
            {
                // Filter by scope type if requested
                var scopeName = scope.Name.ToLowerInvariant();
                if (scopeFilter != "all")
                {
                    if (scopeFilter == "locals" && scopeName != "locals" && scopeName != "local")
                        continue;
                    if (scopeFilter == "arguments" && scopeName != "arguments" && scopeName != "params")
                        continue;
                }

                var variables = await session.Client.GetVariablesAsync(
                    scope.VariablesReference,
                    cancellationToken);

                allVariables.AddRange(variables.Select(v => new
                {
                    name = v.Name,
                    value = v.Value,
                    type = v.Type,
                    scope = scope.Name
                }));
            }

            var result = new
            {
                success = true,
                frameId,
                frameLocation = $"{frames[frameId].Name} at {Path.GetFileName(frames[frameId].Source?.Path ?? "unknown")}:{frames[frameId].Line}",
                scopeFilter,
                variableCount = allVariables.Count,
                variables = allVariables
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to get variables: {ex.Message}");
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

public class GetVariablesArgs
{
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("frameId")]
    public int? FrameId { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
