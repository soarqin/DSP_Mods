using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LiveStreamAssist.Api;

internal sealed class ProducedResponse
{
    public object Result;
    public ApiError Error;
}

internal sealed class PendingRequest
{
    int _done;
    public ApiConnection Connection;
    public string Id;
    readonly TaskCompletionSource<ProducedResponse> _tcs =
        new TaskCompletionSource<ProducedResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<ProducedResponse> Completion => _tcs.Task;
    public bool IsActive => Volatile.Read(ref _done) == 0 && Connection != null && Connection.IsOpen;

    public bool TryComplete(ApiError error)
    {
        if (error == null || Interlocked.Exchange(ref _done, 1) != 0) return false;
        return _tcs.TrySetResult(new ProducedResponse { Error = error });
    }

    public bool TryCompleteResult(object result)
    {
        if (Interlocked.Exchange(ref _done, 1) != 0) return false;
        return _tcs.TrySetResult(new ProducedResponse { Result = result });
    }

    public bool TryCancel()
    {
        if (Interlocked.Exchange(ref _done, 1) != 0) return false;
        _tcs.TrySetCanceled();
        return true;
    }
}

internal sealed class ApiConnection : IDisposable
{
    readonly WebSocketApiServer _server;
    readonly object _gate = new object();
    readonly Dictionary<string, PendingRequest> _outstanding = new Dictionary<string, PendingRequest>(StringComparer.Ordinal);
    readonly Queue<SendItem> _sendQueue = new Queue<SendItem>();
    readonly SemaphoreSlim _sendSignal = new SemaphoreSlim(0);
    readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
    readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
    WebSocket _socket;
    Task _closeTask;
    bool _closing;
    bool _closed;
    bool _sending;

    public ApiConnection(WebSocketApiServer server)
    {
        _server = server;
    }

    public void Attach(WebSocket socket)
    {
        lock (_gate)
        {
            _socket = socket;
            if (!_closed) return;
        }
        socket.Abort();
    }

    public CancellationToken Cancellation => _cancellation.Token;

    public Task CloseCompletion
    {
        get { lock (_gate) return _closeTask ?? Task.CompletedTask; }
    }

    public bool IsOpen
    {
        get
        {
            lock (_gate) return !_closed && !_closing;
        }
    }

    public PendingRequest TryAdmit(string id, out bool duplicate, out bool busy)
    {
        duplicate = false;
        busy = false;
        lock (_gate)
        {
            if (_closed || _closing) return null;
            if (_outstanding.ContainsKey(id))
            {
                duplicate = true;
                return null;
            }

            if (_outstanding.Count >= ApiLimits.MaxPendingRequestsPerConnection || !_server.TryIncrementPending())
            {
                busy = true;
                return null;
            }

            var pending = new PendingRequest { Connection = this, Id = id };
            _outstanding[id] = pending;
            _ = DeliverAsync(pending);
            return pending;
        }
    }

    public bool CanSendBusy()
    {
        lock (_gate)
            return !_closed && !_closing && _sendQueue.Count + (_sending ? 1 : 0) < ApiLimits.MaxPendingRequestsPerConnection;
    }

    public void EnqueueError(object id, ApiError error)
    {
        byte[] payload;
        try
        {
            payload = ApiProtocol.SerializeEnvelope(id, null, error);
        }
        catch (ResponseTooLargeException)
        {
            Close(ApiLimits.CloseTryAgainLater);
            return;
        }

        EnqueueSend(new SendItem { Payload = payload, EnqueuedMs = _server.NowMs });
    }

    public void Release(string id)
    {
        var released = false;
        lock (_gate)
        {
            if (id != null && _outstanding.Remove(id))
            {
                released = true;
            }
        }

        if (released) _server.DecrementPending();
    }

    public void Close(int code) => _ = CloseAsync(code);

    public Task CloseAsync(int code)
    {
        lock (_gate)
        {
            if (_closeTask != null) return _closeTask;
            if (_closed) return Task.CompletedTask;
            _closing = true;
            _sendQueue.Clear();
            _cancellation.CancelAfter(ApiLimits.RequestTimeoutMs);
            _sendSignal.Release();
            return _closeTask = Task.Run(SendCloseAsync);
        }

        async Task SendCloseAsync()
        {
            var entered = false;
            try
            {
                await _sendGate.WaitAsync(Cancellation).ConfigureAwait(false);
                entered = true;
                var ws = _socket;
                if (ws == null)
                {
                    NotifyDisconnected();
                    return;
                }
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    await ws.CloseOutputAsync((WebSocketCloseStatus)code, "", Cancellation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LiveStreamAssist.Logger.LogWarning($"API close failed: {ex.Message}");
                AbortSend(_socket);
            }
            finally
            {
                if (entered) _sendGate.Release();
            }
        }
    }

    public void NotifyDisconnected()
    {
        PendingRequest[] pending;
        lock (_gate)
        {
            if (_closed) return;
            _closed = true;
            pending = SnapshotPending();
            _outstanding.Clear();
            _sendQueue.Clear();
        }

        _server.OnConnectionClosed(this);
        foreach (var item in pending)
        {
            item.TryCancel();
            _server.DecrementPending();
        }
        _cancellation.Cancel();
    }

    public async Task RunSendLoop(WebSocket ws, CancellationToken ct)
    {
        using var disconnected = ct.Register(NotifyDisconnected);
        try
        {
            while (!Cancellation.IsCancellationRequested && IsOpen)
            {
                await _sendSignal.WaitAsync(Cancellation).ConfigureAwait(false);
                SendItem item;
                lock (_gate)
                {
                    if (_closed || _closing) return;
                    if (_sendQueue.Count == 0) continue;
                    item = _sendQueue.Dequeue();
                    _sending = true;
                }

                var remaining = ApiLimits.RequestTimeoutMs - (_server.NowMs - item.EnqueuedMs);
                if (remaining <= 0)
                {
                    AbortSend(ws);
                    return;
                }

                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(Cancellation))
                {
                    timeout.CancelAfter((int)remaining);
                    var entered = false;
                    try
                    {
                        await _sendGate.WaitAsync(timeout.Token).ConfigureAwait(false);
                        entered = true;
                        if (!IsOpen) return;
                        await ws.SendAsync(new ArraySegment<byte>(item.Payload), WebSocketMessageType.Text, true,
                            timeout.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LiveStreamAssist.Logger.LogWarning($"API send failed: {ex.Message}");
                        AbortSend(ws);
                        return;
                    }
                    finally
                    {
                        if (entered) _sendGate.Release();
                        lock (_gate) _sending = false;
                    }
                }

                if (item.Id != null)
                    Release(item.Id);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogWarning($"API send loop failed: {ex.Message}");
            AbortSend(ws);
        }
    }

    async Task DeliverAsync(PendingRequest pending)
    {
        try
        {
            var produced = await pending.Completion.ConfigureAwait(false);
            if (!IsOpen) return;
            byte[] payload;
            try
            {
                payload = ApiProtocol.SerializeEnvelope(pending.Id, produced.Result, produced.Error);
            }
            catch (ResponseTooLargeException)
            {
                payload = ApiProtocol.SerializeEnvelope(pending.Id, null,
                    ApiErrors.LimitExceeded("Response exceeds maxResponseBytes."));
            }

            EnqueueSend(new SendItem { Payload = payload, Id = pending.Id, EnqueuedMs = _server.NowMs });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogError($"API response serialization failed: {ex}");
            Close(ApiLimits.CloseTryAgainLater);
        }
    }

    void EnqueueSend(SendItem item)
    {
        lock (_gate)
        {
            if (_closed || _closing) return;
            if (_sendQueue.Count + (_sending ? 1 : 0) < ApiLimits.MaxPendingRequestsPerConnection)
            {
                _sendQueue.Enqueue(item);
                _sendSignal.Release();
                return;
            }
        }

        Close(ApiLimits.CloseTryAgainLater);
    }

    void AbortSend(WebSocket ws)
    {
        try { ws?.Abort(); }
        catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"API abort failed: {ex.Message}"); }
        NotifyDisconnected();
    }

    public void Dispose()
    {
        NotifyDisconnected();
        _cancellation.Dispose();
        _sendSignal.Dispose();
        _sendGate.Dispose();
    }

    PendingRequest[] SnapshotPending()
    {
        var pending = new PendingRequest[_outstanding.Count];
        if (_outstanding.Count > 0)
            _outstanding.Values.CopyTo(pending, 0);
        return pending;
    }

    struct SendItem
    {
        public byte[] Payload;
        public string Id;
        public long EnqueuedMs;
    }
}

internal sealed class WebSocketApiServer
{
    static readonly UTF8Encoding Utf8Throw = new UTF8Encoding(false, true);
    readonly IPAddress _address;
    readonly int _port;
    readonly string _modVersion;
    readonly Func<GameVersionSnapshot> _version;
    readonly Func<SessionSnapshot> _session;
    readonly MainThreadDispatcher _dispatcher;
    readonly object _gate = new object();
    readonly List<ApiConnection> _connections = new List<ApiConnection>();
    readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    readonly CancellationTokenSource _cts = new CancellationTokenSource();
    IWebHost _host;
    Task _startTask;
    Task _stopTask;
    int _pending;
    bool _alive;
    bool _stopped;

    public WebSocketApiServer(
        IPAddress address,
        int port,
        string modVersion,
        Func<GameVersionSnapshot> version,
        Func<SessionSnapshot> session,
        MainThreadDispatcher dispatcher)
    {
        _address = address;
        _port = port;
        _modVersion = modVersion;
        _version = version;
        _session = session;
        _dispatcher = dispatcher;
    }

    public long NowMs => _clock.ElapsedMilliseconds;

    public void Start() => StartAsync().GetAwaiter().GetResult();

    public Task StartAsync()
    {
        lock (_gate)
        {
            if (_stopped) return Task.CompletedTask;
            return _startTask ?? (_startTask = Task.Run(StartHostAsync));
        }
    }

    async Task StartHostAsync()
    {
        try
        {
            _cts.Token.ThrowIfCancellationRequested();
            await StartHostCoreAsync().ConfigureAwait(false);
        }
        catch
        {
            _ = StopAsync();
            throw;
        }
    }

    async Task StartHostCoreAsync()
    {
        var host = new WebHostBuilder()
            .UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
            .UseContentRoot(Path.GetDirectoryName(typeof(WebSocketApiServer).Assembly.Location) ?? ".")
            .ConfigureLogging(logging => logging.ClearProviders().AddProvider(new HostLogger()))
            .UseKestrel(options =>
            {
                options.Listen(_address, _port);
                options.AddServerHeader = false;
                options.Limits.MaxConcurrentConnections = ApiLimits.MaxConnections + 2;
                options.Limits.MaxConcurrentUpgradedConnections = ApiLimits.MaxConnections;
                options.Limits.MaxRequestHeadersTotalSize = 16 * 1024;
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
                options.Limits.MaxRequestBodySize = 1024;
            })
            .UseSockets()
            .Configure(app =>
            {
                app.UseWebSockets(new WebSocketOptions { ReceiveBufferSize = 4096 });
                app.Run(HandleHttp);
            })
            .Build();
        lock (_gate)
        {
            _host = host;
            _alive = !_stopped;
        }

        await host.StartAsync(_cts.Token).ConfigureAwait(false);
        lock (_gate)
        {
            if (_stopped) return;
        }
        var displayed = _address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            ? "[" + _address + "]" : _address.ToString();
        LiveStreamAssist.Logger.LogInfo($"WebSocket API listening on ws://{displayed}:{_port}{ApiLimits.Path}");
    }

    public void Stop() => _ = StopAsync();

    public Task StopAsync()
    {
        lock (_gate)
        {
            if (_stopTask != null) return _stopTask;
            _stopped = true;
            _alive = false;
            var conns = _connections.ToArray();
            var startup = _startTask;
            return _stopTask = Task.Run(async () =>
            {
                try { _cts.Cancel(); }
                catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"API cancel failed: {ex.Message}"); }
                foreach (var conn in conns)
                    conn.Close(ApiLimits.CloseGoingAway);
                if (startup != null)
                {
                    try { await startup.ConfigureAwait(false); }
                    catch (Exception) { /* Startup failures are reported by the feature owner. */ }
                }
                IWebHost host;
                lock (_gate)
                {
                    host = _host;
                    _host = null;
                }
                DisposeHost(host);
                _cts.Dispose();
            });
        }
    }

    public bool TryIncrementPending()
    {
        lock (_gate)
        {
            if (!_alive || _pending >= ApiLimits.MaxPendingRequests) return false;
            _pending++;
            return true;
        }
    }

    public void DecrementPending()
    {
        lock (_gate)
        {
            if (_pending > 0) _pending--;
        }
    }

    public void OnConnectionClosed(ApiConnection connection)
    {
        lock (_gate)
            _connections.Remove(connection);
    }

    async Task HandleHttp(HttpContext context)
    {
        bool alive;
        lock (_gate) alive = _alive;
        if (!alive)
        {
            context.Response.StatusCode = 503;
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (!string.Equals(path, ApiLimits.Path, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        ApiConnection conn;
        lock (_gate)
        {
            if (!_alive || _connections.Count >= ApiLimits.MaxConnections)
            {
                context.Response.StatusCode = 503;
                return;
            }

            conn = new ApiConnection(this);
            _connections.Add(conn);
        }

        try
        {
            WebSocket ws;
            using (_cts.Token.Register(context.Abort))
                ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            conn.Attach(ws);
            await RunConnection(ws, conn, context.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            conn.NotifyDisconnected();
            await conn.CloseCompletion.ConfigureAwait(false);
            conn.Dispose();
        }
    }

    async Task RunConnection(WebSocket ws, ApiConnection conn, CancellationToken aborted)
    {
        using (ws)
        {
            var send = conn.RunSendLoop(ws, aborted);
            try
            {
                await ReceiveLoop(ws, conn).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException ex)
            {
                LiveStreamAssist.Logger.LogWarning($"API websocket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                LiveStreamAssist.Logger.LogWarning($"API connection failed: {ex.Message}");
            }
            finally
            {
                conn.NotifyDisconnected();
                try { ws.Abort(); }
                catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"API abort failed: {ex.Message}"); }
                try { await send.ConfigureAwait(false); }
                catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"API send join failed: {ex.Message}"); }
            }
        }
    }

    async Task ReceiveLoop(WebSocket ws, ApiConnection conn)
    {
        var buffer = new byte[4096];
        var payload = new byte[ApiLimits.MaxRequestBytes];
        var used = 0;
        long? messageDeadlineMs = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(conn.Cancellation);
        while ((ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseSent) && !conn.Cancellation.IsCancellationRequested)
        {
            var remaining = messageDeadlineMs.HasValue && conn.IsOpen ? messageDeadlineMs.Value - NowMs : Timeout.Infinite;
            if (messageDeadlineMs.HasValue && conn.IsOpen && remaining <= 0)
            {
                await conn.CloseAsync(ApiLimits.CloseTryAgainLater).ConfigureAwait(false);
                continue;
            }
            timeout.CancelAfter((int)remaining);
            WebSocketReceiveResult result;
            try
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                var status = result.CloseStatus.GetValueOrDefault(WebSocketCloseStatus.NormalClosure);
                if (status == WebSocketCloseStatus.Empty) status = WebSocketCloseStatus.NormalClosure;
                await conn.CloseAsync((int)status).ConfigureAwait(false);
                return;
            }

            if (!conn.IsOpen) continue;
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                await conn.CloseAsync(ApiLimits.CloseUnsupportedData).ConfigureAwait(false);
                continue;
            }

            if (messageDeadlineMs.HasValue && NowMs >= messageDeadlineMs.Value)
            {
                await conn.CloseAsync(ApiLimits.CloseTryAgainLater).ConfigureAwait(false);
                continue;
            }

            if (used > ApiLimits.MaxRequestBytes - result.Count)
            {
                await conn.CloseAsync(ApiLimits.CloseMessageTooBig).ConfigureAwait(false);
                continue;
            }

            Buffer.BlockCopy(buffer, 0, payload, used, result.Count);
            used += result.Count;
            if (!result.EndOfMessage)
            {
                if (!messageDeadlineMs.HasValue) messageDeadlineMs = NowMs + ApiLimits.RequestTimeoutMs;
                continue;
            }
            timeout.CancelAfter(Timeout.Infinite);

            string text;
            try
            {
                text = used == 0 ? "" : Utf8Throw.GetString(payload, 0, used);
            }
            catch (ArgumentException)
            {
                await conn.CloseAsync(ApiLimits.CloseInvalidPayload).ConfigureAwait(false);
                continue;
            }

            used = 0;
            messageDeadlineMs = null;
            HandleMessage(conn, text);
        }
    }

    void HandleMessage(ApiConnection conn, string message)
    {
        if (!conn.IsOpen) return;
        var parsed = ApiProtocol.TryParseMessage(message, out var request, out var parseError, out var id);
        if (!parsed && !(id is string))
        {
            conn.EnqueueError(id, parseError);
            return;
        }

        var requestId = parsed ? request.Id : (string)id;
        var admittedMs = NowMs;
        var pending = conn.TryAdmit(requestId, out var duplicate, out var busy);
        if (pending == null)
        {
            if (duplicate)
            {
                conn.Close(ApiLimits.ClosePolicyViolation);
                return;
            }

            if (busy)
            {
                if (conn.CanSendBusy())
                    conn.EnqueueError(requestId, ApiErrors.ServerBusy());
                else
                    conn.Close(ApiLimits.CloseTryAgainLater);
            }

            return;
        }

        try
        {
            if (!parsed)
            {
                pending.TryComplete(parseError);
                return;
            }
            if (!ApiProtocol.IsSystemMethod(request.Method) && !ApiProtocol.IsDataMethod(request.Method))
            {
                pending.TryComplete(ApiErrors.MethodNotFound());
                return;
            }

            if (ApiProtocol.IsSystemMethod(request.Method))
            {
                var handled = ApiProtocol.HandleSystem(request.Method, request.Params, _version(), _modVersion);
                if (handled is ApiError sysErr)
                    pending.TryComplete(sysErr);
                else
                    pending.TryCompleteResult(handled);
                return;
            }

            if (!ApiProtocol.TryParseDataCall(request, out var call, out var callErr))
            {
                pending.TryComplete(callErr);
                return;
            }

            if (call.Method != DataMethod.Roots && !WebSocketApiFeature.IsKnownRoot(call.Root))
            {
                pending.TryComplete(ApiErrors.RootNotFound(call.Root));
                return;
            }

            var session = _session();
            var work = new GameWork
            {
                Pending = pending,
                Call = call,
                Generation = session.Generation,
                DeadlineMs = admittedMs + ApiLimits.RequestTimeoutMs
            };
            if (!_dispatcher.TryEnqueue(work))
                pending.TryComplete(ApiErrors.ServerBusy());
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogError($"API request handling failed: {ex}");
            pending.TryComplete(ApiErrors.InternalError());
        }
    }

    static void DisposeHost(IWebHost host)
    {
        if (host == null) return;
        try
        {
            host.StopAsync(TimeSpan.FromMilliseconds(ApiLimits.RequestTimeoutMs)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogWarning($"WebSocket API host stop failed: {ex.Message}");
        }

        try { host.Dispose(); }
        catch (Exception ex) { LiveStreamAssist.Logger.LogWarning($"WebSocket API host dispose failed: {ex.Message}"); }
    }

    sealed class HostLogger : ILoggerProvider, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning && level != LogLevel.None;
        public void Dispose() { }

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(level)) return;
            var message = "WebSocket host: " + formatter(state, exception);
            if (exception != null) message += "\n" + exception;
            if (level >= LogLevel.Error) LiveStreamAssist.Logger.LogError(message);
            else LiveStreamAssist.Logger.LogWarning(message);
        }
    }
}
