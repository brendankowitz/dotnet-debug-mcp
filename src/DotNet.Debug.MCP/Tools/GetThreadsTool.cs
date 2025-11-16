using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to list all threads in the debugged application
/// </summary>
public class GetThreadsTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public GetThreadsTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_get_threads",
        Description = "Get a list of all threads in the debugged application. Essential for debugging multi-threaded applications and understanding concurrency issues.",
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
            var args = JsonSerializer.Deserialize<GetThreadsArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}");
            }

            if (!session.IsStopped && !session.HasExited)
            {
                return ErrorResult("Program is running. Pause or hit a breakpoint to inspect threads.");
            }

            var threads = await session.Client.GetThreadsAsync(cancellationToken);

            var threadInfo = threads.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                isCurrentThread = t.Id == session.LastStopThreadId
            }).ToList();

            var result = new
            {
                success = true,
                threadCount = threads.Length,
                currentThreadId = session.LastStopThreadId,
                threads = threadInfo
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to get threads: {ex.Message}");
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

public class GetThreadsArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
