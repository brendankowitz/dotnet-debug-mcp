using DotNet.Debug.MCP;

// .NET Debug MCP Server
// An MCP server that enables LLMs to programmatically debug .NET applications

// Get optional NetCoreDbg path from environment
var netCoreDbgPath = Environment.GetEnvironmentVariable("NETCOREDBG_PATH");

// Create and run the MCP server
using var server = new McpServer(
    input: Console.In,
    output: Console.Out,
    errorLog: Console.Error,
    netCoreDbgPath: netCoreDbgPath
);

// Handle Ctrl+C gracefully
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await server.RunAsync(cts.Token);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    return 1;
}

return 0;
