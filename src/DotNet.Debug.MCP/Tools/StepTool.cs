using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to step through code
/// </summary>
public class StepTool : ITool
{
    private readonly DebugSessionManager _sessionManager;
    private readonly string _stepType;

    public StepTool(DebugSessionManager sessionManager, string stepType)
    {
        _sessionManager = sessionManager;
        _stepType = stepType;
    }

    public McpToolDefinition Definition => _stepType switch
    {
        "over" => new()
        {
            Name = "debug_step_over",
            Description = "Step over the current line. This executes the current line and stops at the next line in the same function. Function calls are executed completely without stepping into them.",
            InputSchema = CreateSchema()
        },
        "into" => new()
        {
            Name = "debug_step_into",
            Description = "Step into the current line. If the current line contains a function call, this steps into that function. Otherwise, it behaves like step over.",
            InputSchema = CreateSchema()
        },
        "out" => new()
        {
            Name = "debug_step_out",
            Description = "Step out of the current function. Execution continues until the current function returns to its caller.",
            InputSchema = CreateSchema()
        },
        _ => throw new ArgumentException($"Invalid step type: {_stepType}")
    };

    private static McpInputSchema CreateSchema() => new()
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
                Description = "Thread ID to step (defaults to last stopped thread)"
            }
        },
        Required = Array.Empty<string>()
    };

    public async Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters ?? new { });
            var args = JsonSerializer.Deserialize<StepArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}");
            }

            if (!session.IsStopped)
            {
                return ErrorResult("Program is not stopped. Cannot step.");
            }

            var threadId = args?.ThreadId ?? session.LastStopThreadId ?? 1;

            // Execute step command
            switch (_stepType)
            {
                case "over":
                    await session.Client.NextAsync(threadId, cancellationToken);
                    break;
                case "into":
                    await session.Client.StepInAsync(threadId, cancellationToken);
                    break;
                case "out":
                    await session.Client.StepOutAsync(threadId, cancellationToken);
                    break;
            }

            // Wait for step to complete
            await Task.Delay(500, cancellationToken);

            var result = new
            {
                success = true,
                stepType = _stepType,
                isStopped = session.IsStopped,
                hasExited = session.HasExited,
                stopReason = session.LastStopReason,
                threadId = session.LastStopThreadId,
                message = session.HasExited
                    ? $"Program exited during step"
                    : $"Step {_stepType} completed"
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to step {_stepType}: {ex.Message}");
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

public class StepArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("threadId")]
    public int? ThreadId { get; set; }
}
