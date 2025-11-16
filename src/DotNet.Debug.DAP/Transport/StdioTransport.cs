using System.Diagnostics;
using System.Text;

namespace DotNet.Debug.DAP.Transport;

/// <summary>
/// Transport layer for DAP communication over stdin/stdout
/// Used when launching netcoredbg as a child process
/// </summary>
public class StdioTransport : ITransport
{
    private readonly Process _process;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly CancellationTokenSource _disposalCts = new();
    private Task? _readLoopTask;

    public bool IsConnected => !_process.HasExited;

    public event EventHandler<string>? MessageReceived;

    public StdioTransport(Process process)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _reader = process.StandardOutput;
        _writer = process.StandardInput;

        // Start background read loop for async events
        _readLoopTask = Task.Run(ReadLoopAsync);
    }

    public static async Task<StdioTransport> CreateAsync(
        string netCoreDbgPath,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = netCoreDbgPath,
            Arguments = "--interpreter=vscode",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start netcoredbg process");

        // Wait a moment for process to initialize
        await Task.Delay(100, cancellationToken);

        if (process.HasExited)
        {
            throw new InvalidOperationException(
                $"netcoredbg process exited immediately with code {process.ExitCode}");
        }

        return new StdioTransport(process);
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected");
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);
        var header = $"Content-Length: {messageBytes.Length}\r\n\r\n";

        await _writer.WriteAsync(header);
        await _writer.WriteAsync(message);
        await _writer.FlushAsync();
    }

    public async Task<string?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return null;
        }

        // Read headers
        var headers = new Dictionary<string, string>();
        string? line;

        while ((line = await _reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrEmpty(line))
            {
                // Empty line marks end of headers
                break;
            }

            var parts = line.Split(':', 2);
            if (parts.Length == 2)
            {
                headers[parts[0].Trim()] = parts[1].Trim();
            }
        }

        if (!headers.TryGetValue("Content-Length", out var lengthStr) ||
            !int.TryParse(lengthStr, out var contentLength))
        {
            return null;
        }

        // Read content
        var buffer = new char[contentLength];
        var totalRead = 0;

        while (totalRead < contentLength)
        {
            var read = await _reader.ReadAsync(
                buffer.AsMemory(totalRead, contentLength - totalRead),
                cancellationToken);

            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading message content");
            }

            totalRead += read;
        }

        return new string(buffer);
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_disposalCts.Token.IsCancellationRequested && IsConnected)
            {
                var message = await ReceiveAsync(_disposalCts.Token);
                if (message != null)
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when disposing
        }
        catch (Exception)
        {
            // Log or handle errors
        }
    }

    public void Dispose()
    {
        _disposalCts.Cancel();
        _readLoopTask?.Wait(TimeSpan.FromSeconds(1));

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        _process.Dispose();
        _disposalCts.Dispose();
    }
}
