using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Debug.DAP.Protocol;
using DotNet.Debug.DAP.Protocol.Messages;
using DotNet.Debug.DAP.Transport;

namespace DotNet.Debug.DAP;

/// <summary>
/// Client for communicating with a DAP-compliant debugger (NetCoreDbg)
/// </summary>
public class DAPClient : IDisposable
{
    private readonly ITransport _transport;
    private int _sequenceNumber = 0;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<DAPResponse>> _pendingRequests = new();
    private readonly JsonSerializerOptions _jsonOptions;

    // Event handlers
    public event EventHandler<DAPEvent<StoppedEventBody>>? Stopped;
    public event EventHandler<DAPEvent<ContinuedEventBody>>? Continued;
    public event EventHandler<DAPEvent<ExitedEventBody>>? Exited;
    public event EventHandler<DAPEvent<TerminatedEventBody>>? Terminated;
    public event EventHandler<DAPEvent<ThreadEventBody>>? Thread;
    public event EventHandler<DAPEvent<OutputEventBody>>? Output;
    public event EventHandler<DAPEvent<BreakpointEventBody>>? Breakpoint;
    public event EventHandler<DAPEvent<ModuleEventBody>>? Module;

    public DAPClient(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _transport.MessageReceived += OnMessageReceived;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Create a DAP client connected to NetCoreDbg via stdio
    /// </summary>
    public static async Task<DAPClient> CreateStdioAsync(
        string netCoreDbgPath,
        CancellationToken cancellationToken = default)
    {
        var transport = await StdioTransport.CreateAsync(netCoreDbgPath, cancellationToken);
        return new DAPClient(transport);
    }

    /// <summary>
    /// Initialize the debug adapter
    /// </summary>
    public async Task<Capabilities> InitializeAsync(
        InitializeRequestArguments? args = null,
        CancellationToken cancellationToken = default)
    {
        args ??= new InitializeRequestArguments();

        var response = await SendRequestAsync<InitializeRequestArguments, Capabilities>(
            "initialize",
            args,
            cancellationToken);

        return response.Body ?? new Capabilities();
    }

    /// <summary>
    /// Launch a program
    /// </summary>
    public async Task LaunchAsync(
        LaunchRequestArguments args,
        CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("launch", args, cancellationToken);
    }

    /// <summary>
    /// Attach to a running process
    /// </summary>
    public async Task AttachAsync(
        AttachRequestArguments args,
        CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("attach", args, cancellationToken);
    }

    /// <summary>
    /// Notify debugger that configuration is complete
    /// </summary>
    public async Task ConfigurationDoneAsync(CancellationToken cancellationToken = default)
    {
        await SendRequestAsync("configurationDone", (object?)null, cancellationToken);
    }

    /// <summary>
    /// Set breakpoints for a source file
    /// </summary>
    public async Task<Breakpoint[]> SetBreakpointsAsync(
        string sourceFile,
        SourceBreakpoint[] breakpoints,
        CancellationToken cancellationToken = default)
    {
        var args = new SetBreakpointsArguments
        {
            Source = new Source { Path = sourceFile },
            Breakpoints = breakpoints
        };

        var response = await SendRequestAsync<SetBreakpointsArguments, SetBreakpointsResponseBody>(
            "setBreakpoints",
            args,
            cancellationToken);

        return response.Body?.Breakpoints ?? Array.Empty<Breakpoint>();
    }

    /// <summary>
    /// Continue execution
    /// </summary>
    public async Task<bool> ContinueAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        var args = new ContinueArguments { ThreadId = threadId };

        var response = await SendRequestAsync<ContinueArguments, ContinueResponseBody>(
            "continue",
            args,
            cancellationToken);

        return response.Body?.AllThreadsContinued ?? false;
    }

    /// <summary>
    /// Step over (next line)
    /// </summary>
    public async Task NextAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        var args = new NextArguments { ThreadId = threadId };
        await SendRequestAsync("next", args, cancellationToken);
    }

    /// <summary>
    /// Step into (enter function)
    /// </summary>
    public async Task StepInAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        var args = new StepInArguments { ThreadId = threadId };
        await SendRequestAsync("stepIn", args, cancellationToken);
    }

    /// <summary>
    /// Step out (exit function)
    /// </summary>
    public async Task StepOutAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        var args = new StepOutArguments { ThreadId = threadId };
        await SendRequestAsync("stepOut", args, cancellationToken);
    }

    /// <summary>
    /// Pause execution
    /// </summary>
    public async Task PauseAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        var args = new PauseArguments { ThreadId = threadId };
        await SendRequestAsync("pause", args, cancellationToken);
    }

    /// <summary>
    /// Get stack trace
    /// </summary>
    public async Task<StackFrame[]> GetStackTraceAsync(
        int threadId,
        CancellationToken cancellationToken = default)
    {
        var args = new StackTraceArguments { ThreadId = threadId };

        var response = await SendRequestAsync<StackTraceArguments, StackTraceResponseBody>(
            "stackTrace",
            args,
            cancellationToken);

        return response.Body?.StackFrames ?? Array.Empty<StackFrame>();
    }

    /// <summary>
    /// Get threads
    /// </summary>
    public async Task<Protocol.Messages.Thread[]> GetThreadsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestAsync<object?, ThreadsResponseBody>(
            "threads",
            null,
            cancellationToken);

        return response.Body?.Threads ?? Array.Empty<Protocol.Messages.Thread>();
    }

    /// <summary>
    /// Get scopes for a stack frame
    /// </summary>
    public async Task<Scope[]> GetScopesAsync(
        int frameId,
        CancellationToken cancellationToken = default)
    {
        var args = new ScopesArguments { FrameId = frameId };

        var response = await SendRequestAsync<ScopesArguments, ScopesResponseBody>(
            "scopes",
            args,
            cancellationToken);

        return response.Body?.Scopes ?? Array.Empty<Scope>();
    }

    /// <summary>
    /// Get variables in a scope
    /// </summary>
    public async Task<Variable[]> GetVariablesAsync(
        int variablesReference,
        CancellationToken cancellationToken = default)
    {
        var args = new VariablesArguments { VariablesReference = variablesReference };

        var response = await SendRequestAsync<VariablesArguments, VariablesResponseBody>(
            "variables",
            args,
            cancellationToken);

        return response.Body?.Variables ?? Array.Empty<Variable>();
    }

    /// <summary>
    /// Evaluate an expression
    /// </summary>
    public async Task<EvaluateResponseBody> EvaluateAsync(
        string expression,
        int? frameId = null,
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        var args = new EvaluateArguments
        {
            Expression = expression,
            FrameId = frameId,
            Context = context ?? "repl"
        };

        var response = await SendRequestAsync<EvaluateArguments, EvaluateResponseBody>(
            "evaluate",
            args,
            cancellationToken);

        return response.Body ?? new EvaluateResponseBody();
    }

    /// <summary>
    /// Disconnect from the debugger
    /// </summary>
    public async Task DisconnectAsync(
        bool terminateDebuggee = false,
        CancellationToken cancellationToken = default)
    {
        var args = new { terminateDebuggee };
        await SendRequestAsync("disconnect", args, cancellationToken);
    }

    private async Task<DAPResponse<TBody>> SendRequestAsync<TArgs, TBody>(
        string command,
        TArgs args,
        CancellationToken cancellationToken = default)
        where TBody : class
    {
        var response = await SendRequestAsync(command, args, cancellationToken);

        var typedResponse = new DAPResponse<TBody>
        {
            Seq = response.Seq,
            RequestSeq = response.RequestSeq,
            Success = response.Success,
            Command = response.Command,
            Message = response.Message
        };

        if (response.Body != null)
        {
            // Deserialize the body to the specific type
            var bodyJson = JsonSerializer.Serialize(response.Body, _jsonOptions);
            typedResponse.Body = JsonSerializer.Deserialize<TBody>(bodyJson, _jsonOptions);
        }

        return typedResponse;
    }

    private async Task<DAPResponse> SendRequestAsync(
        string command,
        object? args,
        CancellationToken cancellationToken = default)
    {
        var seq = Interlocked.Increment(ref _sequenceNumber);

        var request = new DAPRequest
        {
            Seq = seq,
            Command = command,
            Arguments = args
        };

        var tcs = new TaskCompletionSource<DAPResponse>();
        _pendingRequests[seq] = tcs;

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            await _transport.SendAsync(json, cancellationToken);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask != tcs.Task)
            {
                throw new TimeoutException($"Request '{command}' timed out after 30 seconds");
            }

            var response = await tcs.Task;

            if (!response.Success)
            {
                throw new DAPException(
                    $"Request '{command}' failed: {response.Message}",
                    response);
            }

            return response;
        }
        finally
        {
            _pendingRequests.TryRemove(seq, out _);
        }
    }

    private void OnMessageReceived(object? sender, string messageJson)
    {
        try
        {
            // Parse the message to determine its type
            var jsonNode = JsonNode.Parse(messageJson);
            var type = jsonNode?["type"]?.GetValue<string>();

            switch (type)
            {
                case "response":
                    HandleResponse(messageJson);
                    break;
                case "event":
                    HandleEvent(messageJson);
                    break;
            }
        }
        catch (Exception)
        {
            // Log error parsing message
        }
    }

    private void HandleResponse(string messageJson)
    {
        var response = JsonSerializer.Deserialize<DAPResponse>(messageJson, _jsonOptions);
        if (response == null) return;

        if (_pendingRequests.TryGetValue(response.RequestSeq, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }

    private void HandleEvent(string messageJson)
    {
        var jsonNode = JsonNode.Parse(messageJson);
        var eventName = jsonNode?["event"]?.GetValue<string>();

        switch (eventName)
        {
            case "stopped":
                var stoppedEvent = JsonSerializer.Deserialize<DAPEvent<StoppedEventBody>>(messageJson, _jsonOptions);
                if (stoppedEvent != null) Stopped?.Invoke(this, stoppedEvent);
                break;

            case "continued":
                var continuedEvent = JsonSerializer.Deserialize<DAPEvent<ContinuedEventBody>>(messageJson, _jsonOptions);
                if (continuedEvent != null) Continued?.Invoke(this, continuedEvent);
                break;

            case "exited":
                var exitedEvent = JsonSerializer.Deserialize<DAPEvent<ExitedEventBody>>(messageJson, _jsonOptions);
                if (exitedEvent != null) Exited?.Invoke(this, exitedEvent);
                break;

            case "terminated":
                var terminatedEvent = JsonSerializer.Deserialize<DAPEvent<TerminatedEventBody>>(messageJson, _jsonOptions);
                if (terminatedEvent != null) Terminated?.Invoke(this, terminatedEvent);
                break;

            case "thread":
                var threadEvent = JsonSerializer.Deserialize<DAPEvent<ThreadEventBody>>(messageJson, _jsonOptions);
                if (threadEvent != null) Thread?.Invoke(this, threadEvent);
                break;

            case "output":
                var outputEvent = JsonSerializer.Deserialize<DAPEvent<OutputEventBody>>(messageJson, _jsonOptions);
                if (outputEvent != null) Output?.Invoke(this, outputEvent);
                break;

            case "breakpoint":
                var breakpointEvent = JsonSerializer.Deserialize<DAPEvent<BreakpointEventBody>>(messageJson, _jsonOptions);
                if (breakpointEvent != null) Breakpoint?.Invoke(this, breakpointEvent);
                break;

            case "module":
                var moduleEvent = JsonSerializer.Deserialize<DAPEvent<ModuleEventBody>>(messageJson, _jsonOptions);
                if (moduleEvent != null) Module?.Invoke(this, moduleEvent);
                break;
        }
    }

    public void Dispose()
    {
        _transport?.Dispose();
    }
}

/// <summary>
/// Exception thrown when a DAP request fails
/// </summary>
public class DAPException : Exception
{
    public DAPResponse Response { get; }

    public DAPException(string message, DAPResponse response)
        : base(message)
    {
        Response = response;
    }
}
