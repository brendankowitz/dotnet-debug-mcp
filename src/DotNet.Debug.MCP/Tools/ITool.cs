using DotNet.Debug.MCP.Protocol;

namespace DotNet.Debug.MCP.Tools;

/// <summary>
/// Interface for MCP tools
/// </summary>
public interface ITool
{
    /// <summary>
    /// Tool definition for MCP
    /// </summary>
    McpToolDefinition Definition { get; }

    /// <summary>
    /// Execute the tool
    /// </summary>
    Task<ToolResult> ExecuteAsync(object? parameters, CancellationToken cancellationToken = default);
}
