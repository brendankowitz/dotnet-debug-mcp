#!/bin/bash
set -e

# Test script for MCP server
# This demonstrates the complete debugging workflow

echo "=== MCP Server End-to-End Test ==="
echo ""

# Check prerequisites
if ! command -v netcoredbg &> /dev/null; then
    echo "⚠ Warning: netcoredbg not found in PATH"
    echo "The MCP server will search common locations, but you may need to install it."
    echo ""
fi

# Build everything
echo "Building projects..."
dotnet build /home/user/dotnet-debug-mcp/dotnet-debug-mcp.sln
echo "✓ Build complete"
echo ""

# Get paths
MCP_DLL="/home/user/dotnet-debug-mcp/src/DotNet.Debug.MCP/bin/Debug/net9.0/DotNet.Debug.MCP.dll"
TEST_DLL="/home/user/dotnet-debug-mcp/src/TestApp/bin/Debug/net9.0/TestApp.dll"
TEST_SRC="/home/user/dotnet-debug-mcp/src/TestApp"

echo "MCP Server: $MCP_DLL"
echo "Test App: $TEST_DLL"
echo ""

# Create a test script that sends MCP commands
cat > /tmp/mcp-test-commands.txt <<'EOF'
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0"}}}
{"jsonrpc":"2.0","id":2,"method":"tools/list"}
EOF

echo "=== Test 1: Initialize and List Tools ==="
echo "Sending initialize and tools/list commands..."
cat /tmp/mcp-test-commands.txt | dotnet "$MCP_DLL" 2>&1 | head -20
echo ""

echo "=== Test 2: Manual MCP Session ==="
echo "You can manually test the MCP server by running:"
echo ""
echo "  dotnet \"$MCP_DLL\""
echo ""
echo "Then send JSON-RPC messages like:"
echo ""
echo "1. Initialize:"
echo '  {"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}'
echo ""
echo "2. List tools:"
echo '  {"jsonrpc":"2.0","id":2,"method":"tools/list"}'
echo ""
echo "3. Start debugging:"
cat <<EOF
  {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"debug_start","arguments":{"program":"$TEST_DLL","args":["logic"]}}}
EOF
echo ""
echo "4. Set breakpoint:"
cat <<EOF
  {"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"debug_set_breakpoint","arguments":{"file":"$TEST_SRC/Calculator.cs","line":29}}}
EOF
echo ""
echo "5. Continue execution:"
echo '  {"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"debug_continue"}}'
echo ""
echo "6. Get stack trace:"
echo '  {"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"debug_get_stack_trace"}}'
echo ""
echo "7. Get variables:"
echo '  {"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"debug_get_variables"}}'
echo ""
echo "8. Evaluate expression:"
echo '  {"jsonrpc":"2.0","id":8,"method":"tools/call","params":{"name":"debug_evaluate","arguments":{"expression":"i"}}}'
echo ""
echo "9. Stop debugging:"
echo '  {"jsonrpc":"2.0","id":9,"method":"tools/call","params":{"name":"debug_stop"}}'
echo ""

echo "=== Available Tools ==="
echo "The MCP server provides these debugging tools:"
echo "  - debug_start           : Start debugging session"
echo "  - debug_set_breakpoint  : Set breakpoint at file:line"
echo "  - debug_continue        : Continue execution"
echo "  - debug_step_over       : Step over (next line)"
echo "  - debug_step_into       : Step into function"
echo "  - debug_step_out        : Step out of function"
echo "  - debug_evaluate        : Evaluate C# expression"
echo "  - debug_get_variables   : Get variables in scope"
echo "  - debug_get_stack_trace : Get call stack"
echo "  - debug_stop            : Stop debugging session"
echo ""

echo "=== Claude Desktop Configuration ==="
echo "To use this with Claude Desktop, add to your MCP settings:"
echo ""
cat <<EOF
{
  "mcpServers": {
    "dotnet-debug": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/user/dotnet-debug-mcp/src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj"
      ]
    }
  }
}
EOF
echo ""

echo "=== Test Complete ==="
