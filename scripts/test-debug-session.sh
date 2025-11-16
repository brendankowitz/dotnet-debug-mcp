#!/bin/bash
set -e

# Simple test script to verify DAP communication with NetCoreDbg
# This script manually tests the debugging workflow

echo "=== NetCoreDbg + TestApp Debug Session Test ==="
echo ""

# Check if netcoredbg is installed
if ! command -v netcoredbg &> /dev/null; then
    echo "❌ netcoredbg not found. Please install it first:"
    echo "   ./scripts/install-netcoredbg.sh"
    exit 1
fi

echo "✓ NetCoreDbg found"
netcoredbg --version
echo ""

# Build TestApp
echo "Building TestApp..."
dotnet build src/TestApp/TestApp.csproj -c Debug
echo "✓ TestApp built"
echo ""

# Get the path to the built DLL
TEST_APP_DLL="$(pwd)/src/TestApp/bin/Debug/net9.0/TestApp.dll"

if [ ! -f "${TEST_APP_DLL}" ]; then
    echo "❌ TestApp.dll not found at ${TEST_APP_DLL}"
    exit 1
fi

echo "✓ Found TestApp.dll: ${TEST_APP_DLL}"
echo ""

# Test 1: Run TestApp with logic error
echo "=== Test 1: Logic Error (Off-by-One) ==="
dotnet "${TEST_APP_DLL}" logic
echo ""

# Test 2: Run with division by zero (will crash)
echo "=== Test 2: Division by Zero (Expected to crash) ==="
dotnet "${TEST_APP_DLL}" divide || echo "Expected crash occurred"
echo ""

# Test 3: Run all tests
echo "=== Test 3: All Tests ==="
dotnet "${TEST_APP_DLL}" all
echo ""

echo "=== Manual NetCoreDbg Test ==="
echo "To manually test NetCoreDbg with DAP protocol:"
echo ""
echo "1. Start NetCoreDbg in DAP mode:"
echo "   netcoredbg --interpreter=vscode"
echo ""
echo "2. Send initialize request (JSON):"
echo '   {"seq":1,"type":"request","command":"initialize","arguments":{"adapterID":"coreclr"}}'
echo ""
echo "3. Send launch request:"
echo "   {\"seq\":2,\"type\":\"request\",\"command\":\"launch\",\"arguments\":{\"program\":\"${TEST_APP_DLL}\",\"args\":[\"logic\"]}}"
echo ""
echo "Note: DAP uses Content-Length headers. See DAP spec for details."
echo ""

echo "=== Test Complete ==="
echo ""
echo "Next steps:"
echo "  - Implement MCP server to wrap these debugging capabilities"
echo "  - Create automated tests using the DAP client library"
echo "  - Build example debugging scenarios"
