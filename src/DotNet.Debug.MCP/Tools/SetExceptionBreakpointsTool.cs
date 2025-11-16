using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to configure exception breakpoints
/// </summary>
public class SetExceptionBreakpointsTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public SetExceptionBreakpointsTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_set_exception_breakpoints",
        Description = "Configure the debugger to break when exceptions are thrown. Useful for catching exceptions early before they propagate up the call stack.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["filters"] = new()
                {
                    Type = "array",
                    Description = "Exception filters to enable (e.g., 'user-unhandled', 'all', 'never')",
                    Items = new McpProperty
                    {
                        Type = "string",
                        Enum = new[] { "all", "user-unhandled", "never" }
                    },
                    Default = new[] { "user-unhandled" }
                },
                ["exceptionTypes"] = new()
                {
                    Type = "array",
                    Description = "Specific exception type names to break on (e.g., 'System.NullReferenceException')",
                    Items = new McpProperty { Type = "string" }
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
            var args = JsonSerializer.Deserialize<SetExceptionBreakpointsArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}. Use debug_start first.");
            }

            // Build exception breakpoints request
            var filters = args?.Filters ?? new[] { "user-unhandled" };
            var exceptionTypes = args?.ExceptionTypes;

            // Create exception options for specific types
            DotNet.Debug.DAP.Protocol.Messages.ExceptionOptions[]? exceptionOptions = null;
            if (exceptionTypes != null && exceptionTypes.Length > 0)
            {
                exceptionOptions = exceptionTypes.Select(et => new DotNet.Debug.DAP.Protocol.Messages.ExceptionOptions
                {
                    BreakMode = "always",
                    Path = new[]
                    {
                        new DotNet.Debug.DAP.Protocol.Messages.ExceptionPathSegment
                        {
                            Names = new[] { et }
                        }
                    }
                }).ToArray();
            }

            // Use DAP client to set exception breakpoints
            var breakpoints = await session.Client.SetExceptionBreakpointsAsync(
                filters,
                exceptionOptions,
                cancellationToken);

            var result = new
            {
                success = true,
                message = $"Exception breakpoints configured",
                filters,
                exceptionTypes,
                breakpointCount = breakpoints.Length,
                info = new
                {
                    note = "Exception breakpoints are now active",
                    filters = string.Join(", ", filters),
                    specificTypes = exceptionTypes != null ? string.Join(", ", exceptionTypes) : "none"
                }
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to set exception breakpoints: {ex.Message}");
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

public class SetExceptionBreakpointsArgs
{
    [JsonPropertyName("filters")]
    public string[]? Filters { get; set; }

    [JsonPropertyName("exceptionTypes")]
    public string[]? ExceptionTypes { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
