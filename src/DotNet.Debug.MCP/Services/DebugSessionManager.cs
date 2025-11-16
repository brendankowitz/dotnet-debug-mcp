using System.Collections.Concurrent;
using DotNet.Debug.DAP;
using DotNet.Debug.DAP.Protocol.Messages;

namespace DotNet.Debug.MCP.Services;

/// <summary>
/// Manages debugging sessions
/// </summary>
public class DebugSessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, DebugSession> _sessions = new();
    private readonly string _netCoreDbgPath;

    public DebugSessionManager(string? netCoreDbgPath = null)
    {
        _netCoreDbgPath = netCoreDbgPath ?? FindNetCoreDbg();
    }

    /// <summary>
    /// Create a new debugging session
    /// </summary>
    public async Task<DebugSession> CreateSessionAsync(
        string sessionId,
        string program,
        string[]? args = null,
        string? cwd = null,
        bool stopAtEntry = false,
        CancellationToken cancellationToken = default)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            throw new InvalidOperationException($"Session {sessionId} already exists");
        }

        var session = new DebugSession
        {
            SessionId = sessionId,
            Program = program,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Create DAP client
            session.Client = await DAPClient.CreateStdioAsync(_netCoreDbgPath, cancellationToken);

            // Set up event handlers
            session.Client.Stopped += (s, e) =>
            {
                session.LastStopReason = e.Body?.Reason;
                session.LastStopThreadId = e.Body?.ThreadId;
                session.IsStopped = true;
            };

            session.Client.Continued += (s, e) =>
            {
                session.IsStopped = false;
            };

            session.Client.Exited += (s, e) =>
            {
                session.ExitCode = e.Body?.ExitCode;
                session.HasExited = true;
            };

            session.Client.Terminated += (s, e) =>
            {
                session.HasExited = true;
            };

            // Initialize
            var capabilities = await session.Client.InitializeAsync(cancellationToken: cancellationToken);
            session.Capabilities = capabilities;

            // Launch
            await session.Client.LaunchAsync(new LaunchRequestArguments
            {
                Program = program,
                Args = args,
                Cwd = cwd ?? Path.GetDirectoryName(program),
                StopAtEntry = stopAtEntry
            }, cancellationToken);

            // Configuration done
            await session.Client.ConfigurationDoneAsync(cancellationToken);

            session.IsLaunched = true;
            _sessions[sessionId] = session;

            return session;
        }
        catch
        {
            session.Client?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Get an existing session
    /// </summary>
    public DebugSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Get or create a default session
    /// </summary>
    public async Task<DebugSession> GetOrCreateDefaultSessionAsync(
        string program,
        string[]? args = null,
        string? cwd = null,
        CancellationToken cancellationToken = default)
    {
        const string defaultSessionId = "default";

        if (_sessions.TryGetValue(defaultSessionId, out var existing))
        {
            return existing;
        }

        return await CreateSessionAsync(defaultSessionId, program, args, cwd, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Stop and remove a session
    /// </summary>
    public async Task StopSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            try
            {
                if (session.Client != null && !session.HasExited)
                {
                    await session.Client.DisconnectAsync(terminateDebuggee: true, cancellationToken);
                }
            }
            finally
            {
                session.Client?.Dispose();
            }
        }
    }

    /// <summary>
    /// List all active sessions
    /// </summary>
    public IEnumerable<string> ListSessions()
    {
        return _sessions.Keys;
    }

    private static string FindNetCoreDbg()
    {
        // Check common locations
        var paths = new[]
        {
            "/usr/local/bin/netcoredbg",
            "/usr/bin/netcoredbg",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin/netcoredbg")
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // Try PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(dir, "netcoredbg");
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        throw new FileNotFoundException(
            "NetCoreDbg not found. Please install it using: ./scripts/install-netcoredbg.sh");
    }

    public void Dispose()
    {
        foreach (var sessionId in _sessions.Keys.ToList())
        {
            StopSessionAsync(sessionId).Wait(TimeSpan.FromSeconds(5));
        }
    }
}

/// <summary>
/// Represents an active debugging session
/// </summary>
public class DebugSession
{
    public string SessionId { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public DAPClient? Client { get; set; }
    public Capabilities? Capabilities { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLaunched { get; set; }
    public bool IsStopped { get; set; }
    public bool HasExited { get; set; }
    public int? ExitCode { get; set; }
    public string? LastStopReason { get; set; }
    public int? LastStopThreadId { get; set; }
}
