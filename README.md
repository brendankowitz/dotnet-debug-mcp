# .NET Debug MCP Server

An MCP (Model Context Protocol) server that enables LLMs to programmatically control a .NET debugger using **NetCoreDbg** and the **Debug Adapter Protocol (DAP)**.

## Overview

This project allows AI assistants like Claude to:
- Start debugging sessions for .NET applications
- Set and manage breakpoints
- Step through code execution
- Inspect variables and evaluate expressions
- Analyze stack traces and program state
- Automatically diagnose and fix bugs

## Architecture

```
┌─────────────────────────────────────────────────┐
│          MCP Client (Claude, etc.)              │
└─────────────────────────────────────────────────┘
                     │ MCP Protocol (stdio)
                     ▼
┌─────────────────────────────────────────────────┐
│          DotNet.Debug.MCP Server                │
│  ┌───────────────────────────────────────────┐  │
│  │         MCP Tool Handlers                 │  │
│  │  - debug_start                            │  │
│  │  - debug_set_breakpoint                   │  │
│  │  - debug_continue                         │  │
│  │  - debug_evaluate                         │  │
│  │  - debug_get_variables                    │  │
│  │  - debug_get_stack_trace                  │  │
│  └───────────────────────────────────────────┘  │
│                     │                           │
│  ┌───────────────────────────────────────────┐  │
│  │         DAP Client Library                │  │
│  │  JSON-RPC over stdio/TCP                  │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
                     │ DAP Protocol
                     ▼
┌─────────────────────────────────────────────────┐
│              NetCoreDbg                         │
└─────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────┐
│          Target .NET Application                │
└─────────────────────────────────────────────────┘
```

## Project Structure

```
dotnet-debug-mcp/
├── src/
│   ├── DotNet.Debug.DAP/          # DAP client library
│   │   ├── DAPClient.cs           # Main DAP client
│   │   ├── Protocol/              # DAP message types
│   │   │   ├── DAPMessage.cs
│   │   │   └── Messages/          # Specific message types
│   │   │       ├── InitializeMessages.cs
│   │   │       ├── LaunchMessages.cs
│   │   │       ├── BreakpointMessages.cs
│   │   │       ├── ExecutionMessages.cs
│   │   │       ├── EvaluationMessages.cs
│   │   │       └── EventMessages.cs
│   │   └── Transport/
│   │       ├── ITransport.cs
│   │       └── StdioTransport.cs
│   │
│   ├── DotNet.Debug.MCP/          # MCP server (TODO)
│   │   ├── Program.cs
│   │   ├── McpServer.cs
│   │   └── Tools/                 # MCP tool implementations
│   │
│   └── TestApp/                   # Test application with bugs
│       ├── Program.cs             # Main entry point
│       ├── Calculator.cs          # Buggy calculator
│       └── DataProcessor.cs       # Null reference bugs
│
├── tests/
│   └── DotNet.Debug.MCP.Tests/
│
├── scripts/
│   ├── install-netcoredbg.sh     # Install NetCoreDbg
│   └── test-debug-session.sh     # Manual testing
│
├── IMPLEMENTATION.md              # Detailed implementation plan
└── README.md                      # This file
```

## Prerequisites

- **.NET 9.0 SDK** or later
- **NetCoreDbg** - .NET debugger implementing DAP
  - Download from: https://github.com/Samsung/netcoredbg/releases
  - Or use the installation script: `./scripts/install-netcoredbg.sh`

## Quick Start

### 1. Install NetCoreDbg

**Ubuntu/Debian:**
```bash
./scripts/install-netcoredbg.sh
```

**Manual Installation:**
```bash
# Download latest release
wget https://github.com/Samsung/netcoredbg/releases/download/VERSION/netcoredbg-linux-amd64.tar.gz

# Extract
tar -xzf netcoredbg-linux-amd64.tar.gz

# Add to PATH
sudo mv netcoredbg /usr/local/bin/
```

**Verify installation:**
```bash
netcoredbg --version
```

### 2. Build the Project

```bash
# Build all projects
dotnet build

# Or build individually
dotnet build src/DotNet.Debug.DAP/DotNet.Debug.DAP.csproj
dotnet build src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj
dotnet build src/TestApp/TestApp.csproj
```

### 3. Test the Test Application

The `TestApp` contains intentional bugs for testing the debugger:

```bash
# Run without arguments to see help
dotnet run --project src/TestApp/TestApp.csproj

# Test specific bugs
dotnet run --project src/TestApp/TestApp.csproj null     # Null reference
dotnet run --project src/TestApp/TestApp.csproj divide   # Division by zero
dotnet run --project src/TestApp/TestApp.csproj index    # Index out of range
dotnet run --project src/TestApp/TestApp.csproj logic    # Off-by-one error
dotnet run --project src/TestApp/TestApp.csproj all      # Run all tests
```

### 4. Run the MCP Server (TODO)

```bash
dotnet run --project src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj
```

## Test Application

The `TestApp` includes several common bug scenarios:

### 1. Null Reference Exception
```bash
dotnet run --project src/TestApp/TestApp.csproj null
```
- **Bug Location:** `DataProcessor.cs:18`
- **Issue:** Missing null check before accessing `input.Length`
- **Expected Error:** `NullReferenceException`

### 2. Division by Zero
```bash
dotnet run --project src/TestApp/TestApp.csproj divide
```
- **Bug Location:** `Calculator.cs:13`
- **Issue:** No validation for divisor being zero
- **Expected Error:** `DivideByZeroException`

### 3. Index Out of Range
```bash
dotnet run --project src/TestApp/TestApp.csproj index
```
- **Bug Location:** `Calculator.cs:21`
- **Issue:** No array bounds checking
- **Expected Error:** `IndexOutOfRangeException`

### 4. Logic Error (Off-by-One)
```bash
dotnet run --project src/TestApp/TestApp.csproj logic
```
- **Bug Location:** `Calculator.cs:29`
- **Issue:** Loop starts at index 1 instead of 0
- **Expected Result:** Sum = 14 (incorrect, should be 15)

### 5. Infinite Loop
```bash
dotnet run --project src/TestApp/TestApp.csproj loop
```
- **Bug Location:** `DataProcessor.cs:34`
- **Issue:** Forgot to increment loop counter
- **Expected Behavior:** Hangs forever (use debugger to break)

## DAP Client Library

The `DotNet.Debug.DAP` library provides a clean C# API for DAP communication:

```csharp
using DotNet.Debug.DAP;
using DotNet.Debug.DAP.Protocol.Messages;

// Create client connected to NetCoreDbg
var client = await DAPClient.CreateStdioAsync("/usr/local/bin/netcoredbg");

// Initialize
await client.InitializeAsync();

// Launch program
await client.LaunchAsync(new LaunchRequestArguments
{
    Program = "/path/to/app.dll",
    Args = new[] { "arg1", "arg2" }
});

// Wait for initialization
await client.ConfigurationDoneAsync();

// Set breakpoint
var breakpoints = await client.SetBreakpointsAsync(
    "/path/to/Program.cs",
    new[] { new SourceBreakpoint { Line = 42 } }
);

// Continue execution
await client.ContinueAsync(threadId: 1);

// Evaluate expression
var result = await client.EvaluateAsync("x + y", frameId: 0);
Console.WriteLine($"Result: {result.Result}");

// Get stack trace
var frames = await client.GetStackTraceAsync(threadId: 1);
foreach (var frame in frames)
{
    Console.WriteLine($"{frame.Name} at {frame.Source?.Path}:{frame.Line}");
}

// Clean up
await client.DisconnectAsync(terminateDebuggee: true);
client.Dispose();
```

## MCP Tools (TODO - Phase 2)

Once the MCP server is implemented, the following tools will be available:

### `debug_start`
Start a debugging session for a .NET application.

```json
{
  "program": "/path/to/app.dll",
  "args": ["arg1", "arg2"],
  "cwd": "/path/to/workdir",
  "stopAtEntry": false
}
```

### `debug_set_breakpoint`
Set a breakpoint at a specific location.

```json
{
  "file": "/path/to/Program.cs",
  "line": 42,
  "condition": "x > 10"
}
```

### `debug_continue`
Continue execution until next breakpoint.

```json
{
  "threadId": 1
}
```

### `debug_step_over` / `debug_step_into` / `debug_step_out`
Step through code execution.

```json
{
  "threadId": 1
}
```

### `debug_evaluate`
Evaluate a C# expression.

```json
{
  "expression": "user.Name",
  "frameId": 0
}
```

### `debug_get_variables`
Get variables in the current scope.

```json
{
  "frameId": 0,
  "scope": "locals"
}
```

### `debug_get_stack_trace`
Get the current call stack.

```json
{
  "threadId": 1
}
```

## Development Status

### ✅ Phase 1: Foundation (Completed)
- [x] Project structure
- [x] Test application with bugs
- [x] DAP protocol message types
- [x] DAP transport layer (stdio)
- [x] DAP client implementation
- [x] Documentation

### 🚧 Phase 2: MCP Server (In Progress)
- [ ] MCP server skeleton
- [ ] MCP tool implementations
- [ ] Integration with DAP client
- [ ] Basic debugging workflow

### 📋 Phase 3: Advanced Features (Planned)
- [ ] Conditional breakpoints
- [ ] Exception breakpoints
- [ ] Watch expressions
- [ ] Multi-threaded debugging
- [ ] Hot reload support

### 📋 Phase 4: Production (Planned)
- [ ] Error handling and recovery
- [ ] Logging and diagnostics
- [ ] Docker containerization
- [ ] CI/CD pipeline
- [ ] Performance optimization

## Testing

```bash
# Run unit tests
dotnet test

# Run test application
dotnet run --project src/TestApp/TestApp.csproj all

# Manual DAP testing (TODO: create test program)
dotnet run --project src/DotNet.Debug.DAP.Tests/DotNet.Debug.DAP.Tests.csproj
```

## Configuration (TODO)

```json
{
  "DebugAdapter": {
    "NetCoreDbgPath": "/usr/local/bin/netcoredbg",
    "DefaultTimeout": 30000,
    "TransportMode": "stdio"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DotNet.Debug.DAP": "Debug"
    }
  }
}
```

## Usage with Claude Desktop (TODO)

Add to your Claude Desktop MCP configuration:

```json
{
  "mcpServers": {
    "dotnet-debug": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/dotnet-debug-mcp/src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj"
      ]
    }
  }
}
```

## Contributing

This is a research project exploring LLM-driven debugging. Contributions are welcome!

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## Resources

- [Debug Adapter Protocol Specification](https://microsoft.github.io/debug-adapter-protocol/)
- [NetCoreDbg GitHub](https://github.com/Samsung/netcoredbg)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [Implementation Document](./IMPLEMENTATION.md)

## License

MIT License - See LICENSE file for details

## Acknowledgments

- **Samsung** for NetCoreDbg
- **Microsoft** for Debug Adapter Protocol
- **Anthropic** for Model Context Protocol

---

**Status:** Phase 1 Complete - DAP Client Implemented ✅

**Next Steps:**
1. Implement MCP server and tools
2. Create end-to-end debugging workflow
3. Add Docker support for easy deployment
4. Build example debugging scenarios
