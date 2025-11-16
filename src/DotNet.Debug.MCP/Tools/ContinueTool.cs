using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to continue execution
/// </summary>
public class ContinueTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public ContinueTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_continue",
        Description = "Continue program execution until the next breakpoint is hit or the program exits. Use this after the program has stopped at a breakpoint to resume normal execution.",
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
                ["threadId"] = new()
                {
                    Type = "integer",
                    Description = "Thread ID to continue (defaults to last stopped thread)"
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
            var args = JsonSerializer.Deserialize<ContinueArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}");
            }

            if (!session.IsStopped)
            {
                return ErrorResult("Program is not stopped. It may already be running or has exited.");
            }

            var threadId = args?.ThreadId ?? session.LastStopThreadId ?? 1;

            // Continue execution
            await session.Client.ContinueAsync(threadId, cancellationToken);

            // Wait a bit for events to come in
            await Task.Delay(500, cancellationToken);

            string status;
            if (session.HasExited)
            {
                status = $"Program exited with code {session.ExitCode ?? 0}";
            }
            else if (session.IsStopped)
            {
                status = $"Stopped: {session.LastStopReason} (thread {session.LastStopThreadId})";
            }
            else
            {
                status = "Running...";
            }

            var result = new
            {
                success = true,
                status,
                isStopped = session.IsStopped,
                hasExited = session.HasExited,
                exitCode = session.ExitCode,
                stopReason = session.LastStopReason,
                threadId = session.LastStopThreadId
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to continue execution: {ex.Message}");
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

public class ContinueArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("threadId")]
    public int? ThreadId { get; set; }
}
