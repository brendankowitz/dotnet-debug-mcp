using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Debug.MCP.Protocol;
using DotNet.Debug.MCP.Services;
using DotNet.Debug.MCP.Tools;

namespace DotNet.Debug.MCP;

/// <summary>
/// MCP Server for .NET debugging
/// </summary>
public class McpServer : IDisposable
{
    private readonly Dictionary<string, ITool> _tools = new();
    private readonly DebugSessionManager _sessionManager;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly TextWriter _errorLog;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServer(
        TextReader? input = null,
        TextWriter? output = null,
        TextWriter? errorLog = null,
        string? netCoreDbgPath = null)
    {
        _input = input ?? Console.In;
        _output = output ?? Console.Out;
        _errorLog = errorLog ?? Console.Error;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        _sessionManager = new DebugSessionManager(netCoreDbgPath);

        // Register all tools
        RegisterTools();
    }

    private void RegisterTools()
    {
        var tools = new ITool[]
        {
            new DebugStartTool(_sessionManager),
            new SetBreakpointTool(_sessionManager),
            new ContinueTool(_sessionManager),
            new StepTool(_sessionManager, "over"),
            new StepTool(_sessionManager, "into"),
            new StepTool(_sessionManager, "out"),
            new EvaluateTool(_sessionManager),
            new GetVariablesTool(_sessionManager),
            new GetStackTraceTool(_sessionManager),
            new StopTool(_sessionManager)
        };

        foreach (var tool in tools)
        {
            _tools[tool.Definition.Name] = tool;
        }

        LogDebug($"Registered {_tools.Count} tools");
    }

    /// <summary>
    /// Run the MCP server (stdio mode)
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        LogDebug("MCP Server starting...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _input.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    LogDebug("Input stream closed");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    await HandleMessageAsync(line, cancellationToken);
                }
                catch (Exception ex)
                {
                    LogDebug($"Error handling message: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogDebug("Server cancelled");
        }
        catch (Exception ex)
        {
            LogDebug($"Server error: {ex}");
        }
        finally
        {
            LogDebug("MCP Server stopping...");
        }
    }

    private async Task HandleMessageAsync(string messageJson, CancellationToken cancellationToken)
    {
        try
        {
            var jsonNode = JsonNode.Parse(messageJson);
            var method = jsonNode?["method"]?.GetValue<string>();
            var id = jsonNode?["id"];

            if (method == null)
            {
                // Response or invalid message - ignore
                return;
            }

            LogDebug($"Received: {method}");

            McpResponse response;

            switch (method)
            {
                case "initialize":
                    response = HandleInitialize(id);
                    break;

                case "tools/list":
                    response = HandleToolsList(id);
                    break;

                case "tools/call":
                    response = await HandleToolsCallAsync(jsonNode, id, cancellationToken);
                    break;

                case "ping":
                    response = new McpResponse
                    {
                        Id = id,
                        Result = new { }
                    };
                    break;

                default:
                    response = new McpResponse
                    {
                        Id = id,
                        Error = new McpError
                        {
                            Code = McpErrorCodes.MethodNotFound,
                            Message = $"Method not found: {method}"
                        }
                    };
                    break;
            }

            await SendResponseAsync(response);
        }
        catch (JsonException ex)
        {
            LogDebug($"JSON parse error: {ex.Message}");
        }
    }

    private McpResponse HandleInitialize(object? id)
    {
        return new McpResponse
        {
            Id = id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new
                {
                    name = "dotnet-debug-mcp",
                    version = "1.0.0"
                },
                capabilities = new
                {
                    tools = new { }
                }
            }
        };
    }

    private McpResponse HandleToolsList(object? id)
    {
        var tools = _tools.Values.Select(t => t.Definition).ToArray();

        return new McpResponse
        {
            Id = id,
            Result = new
            {
                tools
            }
        };
    }

    private async Task<McpResponse> HandleToolsCallAsync(
        JsonNode? messageNode,
        object? id,
        CancellationToken cancellationToken)
    {
        try
        {
            var paramsNode = messageNode?["params"];
            var toolName = paramsNode?["name"]?.GetValue<string>();
            var arguments = paramsNode?["arguments"];

            if (string.IsNullOrEmpty(toolName))
            {
                return new McpResponse
                {
                    Id = id,
                    Error = new McpError
                    {
                        Code = McpErrorCodes.InvalidParams,
                        Message = "Missing tool name"
                    }
                };
            }

            if (!_tools.TryGetValue(toolName, out var tool))
            {
                return new McpResponse
                {
                    Id = id,
                    Error = new McpError
                    {
                        Code = McpErrorCodes.InvalidParams,
                        Message = $"Unknown tool: {toolName}"
                    }
                };
            }

            LogDebug($"Executing tool: {toolName}");

            var toolArgs = arguments?.Deserialize<object>();
            var result = await tool.ExecuteAsync(toolArgs, cancellationToken);

            return new McpResponse
            {
                Id = id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            LogDebug($"Tool execution error: {ex}");

            return new McpResponse
            {
                Id = id,
                Error = new McpError
                {
                    Code = McpErrorCodes.InternalError,
                    Message = $"Tool execution failed: {ex.Message}"
                }
            };
        }
    }

    private async Task SendResponseAsync(McpResponse response)
    {
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        await _output.WriteLineAsync(json);
        await _output.FlushAsync();

        LogDebug($"Sent response: {(response.Error != null ? "ERROR" : "OK")}");
    }

    private void LogDebug(string message)
    {
        try
        {
            _errorLog.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            _errorLog.Flush();
        }
        catch
        {
            // Ignore logging errors
        }
    }

    public void Dispose()
    {
        _sessionManager?.Dispose();
    }
}
