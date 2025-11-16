using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to stop debugging session
/// </summary>
public class StopTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public StopTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_stop",
        Description = "Stop the debugging session and terminate the debugged application. This cleans up resources and ends the debugging session.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["sessionId"] = new()
                {
                    Type = "string",
                    Description = "Session ID to stop (defaults to 'default')"
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
            var args = JsonSerializer.Deserialize<StopArgs>(json);

            var sessionId = args?.SessionId ?? "default";

            await _sessionManager.StopSessionAsync(sessionId, cancellationToken);

            var result = new
            {
                success = true,
                sessionId,
                message = $"Debug session '{sessionId}' stopped successfully"
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to stop session: {ex.Message}");
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

public class StopArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
