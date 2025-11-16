using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to get call stack
/// </summary>
public class GetStackTraceTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public GetStackTraceTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_get_stack_trace",
        Description = "Get the current call stack showing the sequence of function calls that led to the current location. Essential for understanding program flow and diagnosing errors.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["sessionId"] = new()
                {
                    Type = "string",
                    Description = "Session ID (defaults to 'default')"
                },
                ["maxFrames"] = new()
                {
                    Type = "integer",
                    Description = "Maximum number of frames to return (0 = all)",
                    Default = 20
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
            var args = JsonSerializer.Deserialize<GetStackTraceArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}");
            }

            if (!session.IsStopped)
            {
                return ErrorResult("Program is not stopped. Cannot get stack trace.");
            }

            var threadId = session.LastStopThreadId ?? 1;
            var frames = await session.Client.GetStackTraceAsync(threadId, cancellationToken);

            var maxFrames = args?.MaxFrames ?? 20;
            if (maxFrames > 0 && frames.Length > maxFrames)
            {
                frames = frames.Take(maxFrames).ToArray();
            }

            var stackFrames = frames.Select((f, index) => new
            {
                id = f.Id,
                index,
                name = f.Name,
                file = f.Source?.Path ?? "unknown",
                fileName = Path.GetFileName(f.Source?.Path ?? "unknown"),
                line = f.Line,
                column = f.Column
            }).ToList();

            var result = new
            {
                success = true,
                threadId,
                stopReason = session.LastStopReason,
                frameCount = stackFrames.Count,
                frames = stackFrames,
                callStackSummary = string.Join(" -> ", stackFrames.Select(f => f.name))
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to get stack trace: {ex.Message}");
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

public class GetStackTraceArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("maxFrames")]
    public int? MaxFrames { get; set; }
}
