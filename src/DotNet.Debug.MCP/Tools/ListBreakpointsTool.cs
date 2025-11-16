using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to list all active breakpoints
/// </summary>
public class ListBreakpointsTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public ListBreakpointsTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_list_breakpoints",
        Description = "List all currently active breakpoints in the debugging session. Shows file locations, line numbers, and conditions.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
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
            var args = JsonSerializer.Deserialize<ListBreakpointsArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session == null)
            {
                return ErrorResult($"No session found: {sessionId}");
            }

            // Note: This is a simplified version
            // A full implementation would track breakpoints in the session
            var result = new
            {
                success = true,
                sessionId,
                message = "Breakpoint tracking not yet fully implemented",
                note = "Use debug_set_breakpoint to set breakpoints. They are active until debug_stop is called."
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to list breakpoints: {ex.Message}");
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

public class ListBreakpointsArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
