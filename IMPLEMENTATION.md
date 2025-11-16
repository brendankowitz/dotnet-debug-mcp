# .NET Debug MCP Server - Implementation Document

**Project:** dotnet-debug-mcp
**Date:** 2025-01-16
**Status:** In Development

## Overview

This project implements an MCP (Model Context Protocol) server that enables LLMs to programmatically control a .NET debugger using NetCoreDbg and the Debug Adapter Protocol (DAP).

## Architecture

```
┌─────────────────────────────────────────────────┐
│          MCP Client (Claude, etc.)              │
│                                                 │
└─────────────────────────────────────────────────┘
                     │
                     │ MCP Protocol (stdio)
                     ▼
┌─────────────────────────────────────────────────┐
│          DotNet.Debug.MCP Server                │
│  ┌───────────────────────────────────────────┐  │
│  │         MCP Tool Handlers                 │  │
│  │  - debug_start                            │  │
│  │  - debug_stop                             │  │
│  │  - debug_set_breakpoint                   │  │
│  │  - debug_continue                         │  │
│  │  - debug_step_over/into/out               │  │
│  │  - debug_evaluate                         │  │
│  │  - debug_get_variables                    │  │
│  │  - debug_get_stack_trace                  │  │
│  └───────────────────────────────────────────┘  │
│                     │                           │
│  ┌───────────────────────────────────────────┐  │
│  │         DAP Client                        │  │
│  │  - JSON-RPC message handling              │  │
│  │  - Request/Response/Event processing      │  │
│  │  - Session management                     │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
                     │
                     │ DAP Protocol (stdio/TCP)
                     ▼
┌─────────────────────────────────────────────────┐
│              NetCoreDbg                         │
│       --interpreter=vscode                      │
└─────────────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────┐
│          Target .NET Application                │
│              (CoreCLR)                          │
└─────────────────────────────────────────────────┘
```

## Project Structure

```
dotnet-debug-mcp/
├── src/
│   ├── DotNet.Debug.MCP/              # Main MCP server
│   │   ├── Program.cs                 # Entry point
│   │   ├── McpServer.cs               # MCP protocol handler
│   │   ├── Tools/                     # MCP tool implementations
│   │   │   ├── DebugStartTool.cs
│   │   │   ├── DebugStopTool.cs
│   │   │   ├── SetBreakpointTool.cs
│   │   │   ├── ContinueTool.cs
│   │   │   ├── StepTool.cs
│   │   │   ├── EvaluateTool.cs
│   │   │   ├── GetVariablesTool.cs
│   │   │   └── GetStackTraceTool.cs
│   │   └── DotNet.Debug.MCP.csproj
│   │
│   ├── DotNet.Debug.DAP/              # DAP client library
│   │   ├── DAPClient.cs               # Main DAP client
│   │   ├── Protocol/                  # DAP message types
│   │   │   ├── DAPMessage.cs
│   │   │   ├── DAPRequest.cs
│   │   │   ├── DAPResponse.cs
│   │   │   ├── DAPEvent.cs
│   │   │   └── Messages/              # Specific message types
│   │   │       ├── InitializeMessages.cs
│   │   │       ├── LaunchMessages.cs
│   │   │       ├── BreakpointMessages.cs
│   │   │       ├── ExecutionMessages.cs
│   │   │       └── EvaluationMessages.cs
│   │   ├── Transport/
│   │   │   ├── ITransport.cs
│   │   │   ├── StdioTransport.cs
│   │   │   └── TcpTransport.cs
│   │   └── DotNet.Debug.DAP.csproj
│   │
│   └── TestApp/                       # Test application with bugs
│       ├── Program.cs                 # App with intentional issues
│       ├── Calculator.cs              # Buggy calculator
│       ├── DataProcessor.cs           # Null reference bugs
│       └── TestApp.csproj
│
├── tests/
│   └── DotNet.Debug.MCP.Tests/
│       ├── DAPClientTests.cs
│       ├── ToolTests.cs
│       └── DotNet.Debug.MCP.Tests.csproj
│
├── docker/
│   ├── Dockerfile                     # NetCoreDbg + MCP server
│   └── docker-compose.yml
│
├── scripts/
│   ├── install-netcoredbg.sh         # Install NetCoreDbg
│   └── test-debug-session.sh         # Manual testing script
│
├── .gitignore
├── README.md
├── IMPLEMENTATION.md                  # This file
└── dotnet-debug-mcp.sln
```

## MCP Tools Specification

### 1. debug_start

Start a debugging session for a .NET application.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "program": {
      "type": "string",
      "description": "Path to the .NET DLL to debug"
    },
    "args": {
      "type": "array",
      "items": {"type": "string"},
      "description": "Command-line arguments for the application",
      "default": []
    },
    "cwd": {
      "type": "string",
      "description": "Working directory for the application"
    },
    "stopAtEntry": {
      "type": "boolean",
      "description": "Stop at entry point",
      "default": false
    }
  },
  "required": ["program"]
}
```

**Output:**
```json
{
  "success": true,
  "sessionId": "session-uuid",
  "message": "Debug session started for /path/to/app.dll"
}
```

### 2. debug_stop

Stop the current debugging session.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "sessionId": {
      "type": "string",
      "description": "Session ID to stop"
    }
  }
}
```

### 3. debug_set_breakpoint

Set a breakpoint at a specific location.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "file": {
      "type": "string",
      "description": "Source file path (absolute)"
    },
    "line": {
      "type": "integer",
      "description": "Line number (1-based)"
    },
    "condition": {
      "type": "string",
      "description": "Optional breakpoint condition (C# expression)"
    }
  },
  "required": ["file", "line"]
}
```

**Output:**
```json
{
  "success": true,
  "breakpointId": 1,
  "verified": true,
  "line": 42,
  "message": "Breakpoint set at Program.cs:42"
}
```

### 4. debug_continue

Continue execution until next breakpoint or program exit.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "threadId": {
      "type": "integer",
      "description": "Thread to continue (optional, defaults to all)"
    }
  }
}
```

**Output:**
```json
{
  "success": true,
  "stopped": true,
  "reason": "breakpoint",
  "threadId": 1,
  "line": 42,
  "file": "/path/to/Program.cs"
}
```

### 5. debug_step_over / debug_step_into / debug_step_out

Step through code execution.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "threadId": {
      "type": "integer",
      "description": "Thread to step"
    }
  }
}
```

### 6. debug_evaluate

Evaluate a C# expression in the current context.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "expression": {
      "type": "string",
      "description": "C# expression to evaluate"
    },
    "frameId": {
      "type": "integer",
      "description": "Stack frame ID (0 = current frame)",
      "default": 0
    }
  },
  "required": ["expression"]
}
```

**Output:**
```json
{
  "success": true,
  "result": "42",
  "type": "int",
  "variablesReference": 0
}
```

### 7. debug_get_variables

Get variables in the current scope.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "frameId": {
      "type": "integer",
      "description": "Stack frame ID",
      "default": 0
    },
    "scope": {
      "type": "string",
      "enum": ["locals", "arguments", "globals"],
      "description": "Variable scope to inspect"
    }
  }
}
```

**Output:**
```json
{
  "success": true,
  "variables": [
    {
      "name": "x",
      "value": "42",
      "type": "int"
    },
    {
      "name": "message",
      "value": "\"Hello\"",
      "type": "string"
    }
  ]
}
```

### 8. debug_get_stack_trace

Get the current call stack.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "threadId": {
      "type": "integer",
      "description": "Thread ID"
    }
  }
}
```

**Output:**
```json
{
  "success": true,
  "stackFrames": [
    {
      "id": 0,
      "name": "Program.Main",
      "source": "/path/to/Program.cs",
      "line": 42,
      "column": 13
    },
    {
      "id": 1,
      "name": "Calculator.Divide",
      "source": "/path/to/Calculator.cs",
      "line": 15,
      "column": 9
    }
  ]
}
```

## DAP Protocol Implementation

### Message Format

All DAP messages follow the JSON-RPC format with a header:

```
Content-Length: <length>\r\n
\r\n
<json-payload>
```

### Request Example

```json
{
  "seq": 1,
  "type": "request",
  "command": "setBreakpoints",
  "arguments": {
    "source": {
      "path": "/path/to/Program.cs"
    },
    "breakpoints": [
      {"line": 42}
    ]
  }
}
```

### Response Example

```json
{
  "seq": 2,
  "type": "response",
  "request_seq": 1,
  "success": true,
  "command": "setBreakpoints",
  "body": {
    "breakpoints": [
      {
        "id": 1,
        "verified": true,
        "line": 42
      }
    ]
  }
}
```

### Event Example

```json
{
  "seq": 3,
  "type": "event",
  "event": "stopped",
  "body": {
    "reason": "breakpoint",
    "threadId": 1,
    "allThreadsStopped": true
  }
}
```

## Implementation Phases

### Phase 1: Foundation (Current)
- [x] Research and architecture design
- [ ] Project structure setup
- [ ] Test application with bugs
- [ ] Basic DAP client (initialize, launch, attach)
- [ ] Basic MCP server skeleton

### Phase 2: Core Debugging Features
- [ ] Breakpoint management (set, remove, list)
- [ ] Execution control (continue, pause)
- [ ] Step operations (over, into, out)
- [ ] Stack trace retrieval
- [ ] Variable inspection

### Phase 3: Advanced Features
- [ ] Expression evaluation
- [ ] Conditional breakpoints
- [ ] Watch expressions
- [ ] Exception breakpoints
- [ ] Multi-threaded debugging

### Phase 4: Production Readiness
- [ ] Error handling and recovery
- [ ] Session management
- [ ] Logging and diagnostics
- [ ] Docker containerization
- [ ] Documentation and examples

## Test Application Design

The test application will include several common bug scenarios:

1. **Null Reference Exception**
   ```csharp
   public void ProcessData(string? input)
   {
       // Bug: doesn't check for null
       var length = input.Length; // NullReferenceException
   }
   ```

2. **Division by Zero**
   ```csharp
   public int Divide(int a, int b)
   {
       // Bug: doesn't check for zero
       return a / b; // DivideByZeroException
   }
   ```

3. **Index Out of Range**
   ```csharp
   public int GetElement(int[] array, int index)
   {
       // Bug: doesn't validate index
       return array[index]; // IndexOutOfRangeException
   }
   ```

4. **Infinite Loop**
   ```csharp
   public void ProcessItems(List<int> items)
   {
       int i = 0;
       while (i < items.Count)
       {
           // Bug: forgot to increment i
           Console.WriteLine(items[i]);
       }
   }
   ```

5. **Logic Error**
   ```csharp
   public int CalculateSum(int[] numbers)
   {
       int sum = 0;
       for (int i = 1; i < numbers.Length; i++) // Bug: starts at 1 instead of 0
       {
           sum += numbers[i];
       }
       return sum;
   }
   ```

## Dependencies

### NuGet Packages

- **Microsoft.Extensions.Logging** - Logging infrastructure
- **System.Text.Json** - JSON serialization for DAP
- **ModelContextProtocol** - MCP protocol implementation (or manual implementation)

### External Tools

- **NetCoreDbg** - .NET debugger implementing DAP
  - Installation: Download from https://github.com/Samsung/netcoredbg/releases
  - Required for debugging operations

## Configuration

### appsettings.json

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

## Usage Examples

### Starting the MCP Server

```bash
# Using stdio transport (recommended for MCP)
dotnet run --project src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj
```

### MCP Client Configuration (Claude Desktop)

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

### Example Debugging Session

```
User: Debug the test application and find why it's crashing

LLM:
1. [Uses debug_start] Start debugging TestApp.dll
2. [Uses debug_set_breakpoint] Set breakpoint at Program.cs:15
3. [Uses debug_continue] Run until breakpoint
4. [Uses debug_get_variables] Inspect local variables
5. [Uses debug_get_stack_trace] Get call stack
6. [Uses debug_evaluate] Evaluate expression "input == null"
7. Analysis: Found null reference - input parameter is null

The crash is caused by a null reference exception at line 15.
The 'input' parameter is null when ProcessData is called.
```

## Security Considerations

1. **Path Validation**: All file paths must be validated to prevent directory traversal
2. **Expression Evaluation**: Evaluate expressions in sandboxed context
3. **Resource Limits**: Implement timeouts for all debugging operations
4. **Process Isolation**: Debug sessions should be isolated per MCP connection

## Performance Considerations

1. **Async I/O**: All DAP communication should be async
2. **Message Buffering**: Buffer DAP messages to handle rapid events
3. **Connection Pooling**: Reuse NetCoreDbg processes when possible
4. **Memory Management**: Clean up debug sessions promptly

## Error Handling

### DAP Communication Errors
- Connection failures
- Timeout errors
- Invalid responses
- Protocol errors

### Debugging Errors
- Breakpoint verification failures
- Invalid expression evaluation
- Thread/process termination
- Debugger crashes

### MCP Protocol Errors
- Invalid tool parameters
- Session not found
- Tool execution failures

## Testing Strategy

### Unit Tests
- DAP message serialization/deserialization
- Protocol message handling
- Tool parameter validation

### Integration Tests
- End-to-end debugging sessions
- Breakpoint hit verification
- Variable inspection accuracy
- Expression evaluation correctness

### Manual Testing
- Test application debugging scenarios
- Error condition handling
- Performance under load

## Future Enhancements

1. **Remote Debugging**: Support debugging applications on remote machines
2. **Multi-Process**: Debug multiple processes simultaneously
3. **Performance Profiling**: Add CPU/memory profiling tools
4. **Log Point Support**: Non-breaking breakpoints that log expressions
5. **Time-Travel Debugging**: Integration with TTD or rr
6. **Blazor/WASM Support**: Debug Blazor WebAssembly applications
7. **Hot Reload**: Support for hot reload debugging scenarios

## References

- [Debug Adapter Protocol Specification](https://microsoft.github.io/debug-adapter-protocol/)
- [NetCoreDbg Documentation](https://github.com/Samsung/netcoredbg)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [MCP TypeScript SDK](https://github.com/modelcontextprotocol/typescript-sdk)

---

**Status:** Implementation in progress
**Next Steps:** Create project structure and test application
