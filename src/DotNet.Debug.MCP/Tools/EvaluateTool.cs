using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to evaluate expressions
/// </summary>
public class EvaluateTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public EvaluateTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_evaluate",
        Description = "Evaluate a C# expression in the context of the current breakpoint. This allows you to inspect variable values, call methods, and test hypotheses about the code's behavior.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["expression"] = new()
                {
                    Type = "string",
                    Description = "C# expression to evaluate (e.g., 'x + y', 'user.Name', 'array.Length')"
                },
                ["frameId"] = new()
                {
                    Type = "integer",
                    Description = "Stack frame ID (0 = current frame, 1 = caller, etc.)",
                    Default = 0
                },
                ["sessionId"] = new()
                {
                    Type = "string",
                    Description = "Session ID (defaults to 'default')"
                }
            },
            Required = new[] { "expression" }
        }
    };

    public async Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters);
            var args = JsonSerializer.Deserialize<EvaluateArgs>(json);

            if (args == null || string.IsNullOrEmpty(args.Expression))
            {
                return ErrorResult("Missing required parameter: expression");
            }

            var sessionId = args.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}");
            }

            if (!session.IsStopped)
            {
                return ErrorResult("Program is not stopped. Cannot evaluate expressions.");
            }

            // Get thread ID
            var threadId = session.LastStopThreadId ?? 1;

            // Get stack frames to determine frameId
            var frames = await session.Client.GetStackTraceAsync(threadId, cancellationToken);
            if (frames.Length == 0)
            {
                return ErrorResult("No stack frames available");
            }

            var frameId = args.FrameId ?? 0;
            if (frameId >= frames.Length)
            {
                return ErrorResult($"Invalid frame ID {frameId}. Available frames: 0-{frames.Length - 1}");
            }

            // Evaluate expression
            var evalResult = await session.Client.EvaluateAsync(
                args.Expression,
                frames[frameId].Id,
                "repl",
                cancellationToken);

            var result = new
            {
                success = true,
                expression = args.Expression,
                result = evalResult.Result,
                type = evalResult.Type,
                frameId,
                frameLocation = $"{frames[frameId].Name} at {Path.GetFileName(frames[frameId].Source?.Path ?? "unknown")}:{frames[frameId].Line}"
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to evaluate expression: {ex.Message}");
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

public class EvaluateArgs
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("frameId")]
    public int? FrameId { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
