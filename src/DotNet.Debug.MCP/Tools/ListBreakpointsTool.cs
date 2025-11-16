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

    public Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters ?? new { });
            var args = JsonSerializer.Deserialize<ListBreakpointsArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session == null)
            {
                return Task.FromResult(ErrorResult($"No session found: {sessionId}"));
            }

            // Get tracked breakpoints from session
            var breakpoints = session.GetBreakpoints();

            var result = new
            {
                success = true,
                sessionId,
                breakpointCount = breakpoints.Count,
                breakpoints = breakpoints.Select(bp => new
                {
                    id = bp.Id,
                    file = bp.File,
                    line = bp.Line,
                    verified = bp.Verified,
                    condition = bp.Condition
                }).ToList()
            };

            return Task.FromResult(SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to list breakpoints: {ex.Message}"));
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
