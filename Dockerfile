# Multi-stage Dockerfile for .NET Debug MCP Server

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["dotnet-debug-mcp.sln", "./"]
COPY ["src/DotNet.Debug.DAP/DotNet.Debug.DAP.csproj", "src/DotNet.Debug.DAP/"]
COPY ["src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj", "src/DotNet.Debug.MCP/"]

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY ["src/", "src/"]

# Build and publish
RUN dotnet publish src/DotNet.Debug.MCP/DotNet.Debug.MCP.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime with NetCoreDbg
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# Install NetCoreDbg
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://github.com/Samsung/netcoredbg/releases/download/3.1.0-1031/netcoredbg-linux-amd64.tar.gz && \
    tar -xzf netcoredbg-linux-amd64.tar.gz && \
    mv netcoredbg/netcoredbg /usr/local/bin/ && \
    chmod +x /usr/local/bin/netcoredbg && \
    rm -rf netcoredbg* && \
    apt-get remove -y wget && \
    apt-get autoremove -y && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Set environment
ENV NETCOREDBG_PATH=/usr/local/bin/netcoredbg
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Run as non-root user
RUN useradd -m -u 1000 mcpuser && \
    chown -R mcpuser:mcpuser /app

USER mcpuser

ENTRYPOINT ["dotnet", "DotNet.Debug.MCP.dll"]
