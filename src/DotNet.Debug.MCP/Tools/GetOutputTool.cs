using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to get program output and logs
/// </summary>
public class GetOutputTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public GetOutputTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_get_output",
        Description = "Get the captured output from the debugged application (stdout, stderr, and debugger messages). Useful for understanding what the program has printed during execution.",
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
                ["category"] = new()
                {
                    Type = "string",
                    Description = "Output category filter: 'stdout', 'stderr', 'console', or 'all'",
                    Enum = new[] { "stdout", "stderr", "console", "all" },
                    Default = "all"
                },
                ["maxLines"] = new()
                {
                    Type = "integer",
                    Description = "Maximum number of output lines to return",
                    Default = 100
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
            var args = JsonSerializer.Deserialize<GetOutputArgs>(json);

            var sessionId = args?.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session == null)
            {
                return ErrorResult($"No session found: {sessionId}");
            }

            var category = args?.Category ?? "all";
            var maxLines = args?.MaxLines ?? 100;

            // Get output from session
            var output = session.GetOutput(category, maxLines);

            var result = new
            {
                success = true,
                sessionId,
                category,
                lineCount = output.Count,
                output = output.Take(maxLines).ToList()
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to get output: {ex.Message}");
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

public class GetOutputArgs
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("maxLines")]
    public int? MaxLines { get; set; }
}
