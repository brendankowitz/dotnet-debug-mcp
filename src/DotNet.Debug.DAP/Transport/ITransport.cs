namespace DotNet.Debug.DAP.Transport;

/// <summary>
/// Interface for DAP message transport (stdio, TCP, etc.)
/// </summary>
public interface ITransport : IDisposable
{
    /// <summary>
    /// Send a message to the debugger
    /// </summary>
    Task SendAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive a message from the debugger
    /// </summary>
    Task<string?> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the transport is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Event fired when a message is received asynchronously
    /// </summary>
    event EventHandler<string>? MessageReceived;
}
