using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Debug.DAP.Protocol.Messages;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Tool to set breakpoints
/// </summary>
public class SetBreakpointTool : ITool
{
    private readonly DebugSessionManager _sessionManager;

    public SetBreakpointTool(DebugSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public McpToolDefinition Definition => new()
    {
        Name = "debug_set_breakpoint",
        Description = "Set a breakpoint at a specific line in a source file. The debugger will pause execution when this line is reached, allowing you to inspect variables and program state.",
        InputSchema = new McpInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpProperty>
            {
                ["file"] = new()
                {
                    Type = "string",
                    Description = "Absolute path to the source file (e.g., /path/to/Program.cs)"
                },
                ["line"] = new()
                {
                    Type = "integer",
                    Description = "Line number (1-based) where the breakpoint should be set"
                },
                ["condition"] = new()
                {
                    Type = "string",
                    Description = "Optional C# expression that must evaluate to true for the breakpoint to trigger (e.g., 'x > 10')"
                },
                ["sessionId"] = new()
                {
                    Type = "string",
                    Description = "Session ID (defaults to 'default')"
                }
            },
            Required = new[] { "file", "line" }
        }
    };

    public async Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(parameters);
            var args = JsonSerializer.Deserialize<SetBreakpointArgs>(json);

            if (args == null || string.IsNullOrEmpty(args.File) || args.Line == 0)
            {
                return ErrorResult("Missing required parameters: file and line");
            }

            var sessionId = args.SessionId ?? "default";
            var session = _sessionManager.GetSession(sessionId);

            if (session?.Client == null)
            {
                return ErrorResult($"No active session found: {sessionId}. Use debug_start first.");
            }

            // Canonicalize and validate path
            string canonicalPath;
            try
            {
                canonicalPath = Path.GetFullPath(args.File);
                if (!File.Exists(canonicalPath))
                {
                    // Try relative to program directory
                    var programDir = Path.GetDirectoryName(session.Program);
                    if (programDir != null)
                    {
                        var altPath = Path.GetFullPath(Path.Combine(programDir, args.File));
                        if (File.Exists(altPath))
                        {
                            canonicalPath = altPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorResult($"Invalid file path: {ex.Message}");
            }

            var breakpoint = new SourceBreakpoint
            {
                Line = args.Line,
                Condition = args.Condition
            };

            var breakpoints = await session.Client.SetBreakpointsAsync(
                canonicalPath,
                new[] { breakpoint },
                cancellationToken);

            if (breakpoints.Length == 0)
            {
                return ErrorResult("Failed to set breakpoint");
            }

            var bp = breakpoints[0];

            // Track breakpoint in session
            session.AddBreakpoint(new TrackedBreakpoint
            {
                Id = bp.Id,
                File = canonicalPath,
                Line = bp.Line ?? args.Line,
                Verified = bp.Verified,
                Condition = args.Condition
            });

            session.UpdateActivity();
            var result = new
            {
                success = true,
                breakpoint = new
                {
                    id = bp.Id,
                    verified = bp.Verified,
                    line = bp.Line ?? args.Line,
                    file = args.File,
                    condition = args.Condition,
                    message = bp.Message
                },
                message = bp.Verified
                    ? $"Breakpoint set at {Path.GetFileName(args.File)}:{bp.Line ?? args.Line}"
                    : $"Breakpoint set but not verified: {bp.Message}"
            };

            return SuccessResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to set breakpoint: {ex.Message}");
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

public class SetBreakpointArgs
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}
