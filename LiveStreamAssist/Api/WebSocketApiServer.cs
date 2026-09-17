using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json.Linq;

namespace LiveStreamAssist.Api;

internal sealed class PendingRequest
{
    int _done;
    public ApiConnection Connection;
    public string Id;
    public bool IsActive => System.Threading.Volatile.Read(ref _done) == 0 && Connection != null && Connection.IsOpen;

    public bool TryMarkDone() => System.Threading.Interlocked.Exchange(ref _done, 1) == 0;

    public bool Complete(ApiError error)
    {
        if (error == null || !TryMarkDone()) return false;
        Connection.Deliver(this, null, error);
        return true;
    }

    public bool CompleteResult(object result)
    {
        if (!TryMarkDone()) return false;
        Connection.Deliver(this, result, null);
        return true;
    }
}

internal sealed class ApiConnection
{
    readonly WebSocketApiServer _server;
    readonly IWebSocketConnection _socket;
    readonly object _gate = new object();
    readonly Dictionary<string, PendingRequest> _outstanding = new Dictionary<string, PendingRequest>(StringComparer.Ordinal);
    readonly Queue<SendItem> _sendQueue = new Queue<SendItem>();
    bool _sending;
    bool _closed;
    int _pending;

    public ApiConnection(WebSocketApiServer server, IWebSocketConnection socket)
    {
        _server = server;
        _socket = socket;
    }

    public bool IsOpen
    {
        get
        {
            lock (_gate) return !_closed && _socket.IsAvailable;
        }
    }

    public int Pending
    {
        get
        {
            lock (_gate) return _pending;
        }
    }

    public PendingRequest TryAdmit(string id, out bool duplicate, out bool busy)
    {
        duplicate = false;
        busy = false;
        lock (_gate)
        {
            if (_closed) return null;
            if (_outstanding.ContainsKey(id))
            {
                duplicate = true;
                return null;
            }

            if (_pending >= ApiLimits.MaxPendingRequestsPerConnection || !_server.TryIncrementPending())
            {
                busy = true;
                return null;
            }

            var pending = new PendingRequest { Connection = this, Id = id };
            _outstanding[id] = pending;
            _pending++;
            return pending;
        }
    }

    public bool CanSendBusy()
    {
        lock (_gate)
            return !_closed && _sendQueue.Count < ApiLimits.MaxPendingRequestsPerConnection;
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

        EnqueueSend(payload);
    }

    public void Deliver(PendingRequest pending, object result, ApiError error)
    {
        Release(pending.Id);
        if (_closed) return;
        byte[] payload;
        try
        {
            payload = ApiProtocol.SerializeEnvelope(pending.Id, result, error);
        }
        catch (ResponseTooLargeException)
        {
            try
            {
                payload = ApiProtocol.SerializeEnvelope(pending.Id, null, ApiErrors.LimitExceeded("Response exceeds maxResponseBytes."));
            }
            catch (ResponseTooLargeException)
            {
                Close(ApiLimits.CloseTryAgainLater);
                return;
            }
        }

        EnqueueSend(payload);
    }

    public void Release(string id)
    {
        lock (_gate)
        {
            if (id != null)
                _outstanding.Remove(id);
            if (_pending > 0)
                _pending--;
        }

        _server.DecrementPending();
    }

    public void EnqueueSend(byte[] payload)
    {
        lock (_gate)
        {
            if (_closed) return;
            if (_sendQueue.Count >= ApiLimits.MaxPendingRequestsPerConnection)
            {
                CloseLocked(ApiLimits.CloseTryAgainLater);
                return;
            }

            _sendQueue.Enqueue(new SendItem { Payload = payload, EnqueuedMs = _server.NowMs });
            if (!_sending)
            {
                _sending = true;
                DrainSend();
            }
        }
    }

    public void Close(int code)
    {
        lock (_gate) CloseLocked(code);
    }

    public void NotifyDisconnected()
    {
        FinishClose(null);
    }

    void CloseLocked(int code)
    {
        FinishClose(code);
    }

    void FinishClose(int? code)
    {
        PendingRequest[] pending;
        lock (_gate)
        {
            if (_closed) return;
            _closed = true;
            _sendQueue.Clear();
            _sending = false;
            pending = new PendingRequest[_outstanding.Count];
            if (_outstanding.Count > 0)
                _outstanding.Values.CopyTo(pending, 0);
        }

        if (code.HasValue)
        {
            try
            {
                if (_socket.IsAvailable)
                    _socket.Close(code.Value);
            }
            catch (Exception ex)
            {
                LiveStreamAssist.Logger.LogWarning($"API close failed: {ex.Message}");
            }
        }

        _server.OnConnectionClosed(this);
        foreach (var item in pending)
            item.Complete(ApiErrors.RequestTimeout());
    }

    void DrainSend()
    {
        SendItem item;
        lock (_gate)
        {
            if (_closed || _sendQueue.Count == 0)
            {
                _sending = false;
                return;
            }

            item = _sendQueue.Peek();
            if (_server.NowMs - item.EnqueuedMs > ApiLimits.RequestTimeoutMs)
            {
                CloseLocked(ApiLimits.CloseTryAgainLater);
                return;
            }

            _sendQueue.Dequeue();
        }

        Task task;
        try
        {
            var text = Encoding.UTF8.GetString(item.Payload);
            task = _socket.Send(text);
        }
        catch (Exception)
        {
            Close(ApiLimits.CloseTryAgainLater);
            return;
        }

        if (task == null)
        {
            Close(ApiLimits.CloseTryAgainLater);
            return;
        }

        task.ContinueWith(_ => DrainSend(), System.Threading.CancellationToken.None,
            TaskContinuationOptions.None, TaskScheduler.Default);
    }

    struct SendItem
    {
        public byte[] Payload;
        public long EnqueuedMs;
    }
}

internal sealed class WebSocketApiServer
{
    readonly IPAddress _address;
    readonly int _port;
    readonly string _modVersion;
    readonly Func<GameVersionSnapshot> _version;
    readonly Func<SessionSnapshot> _session;
    readonly MainThreadDispatcher _dispatcher;
    readonly object _gate = new object();
    readonly List<ApiConnection> _connections = new List<ApiConnection>();
    readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    WebSocketServer _listener;
    int _pending;
    bool _alive;

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

    public void Start()
    {
        var host = _address.Equals(IPAddress.Any) ? "0.0.0.0" : _address.ToString();
        if (_address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            host = "[" + host + "]";
        var location = "ws://" + host + ":" + _port;
        FleckLog.Level = LogLevel.Error;
        FleckLog.LogAction = (level, message, ex) =>
        {
            if (level == LogLevel.Error)
                LiveStreamAssist.Logger.LogError(ex != null ? message + ": " + ex : message);
        };
        var server = new WebSocketServer(location, false);
        server.RestartAfterListenError = false;
        server.Start(OnSocket);
        lock (_gate)
        {
            _listener = server;
            _alive = true;
        }

        LiveStreamAssist.Logger.LogInfo($"WebSocket API listening on {location}{ApiLimits.Path}");
    }

    public void Stop()
    {
        ApiConnection[] conns;
        WebSocketServer listener;
        lock (_gate)
        {
            if (!_alive && _listener == null) return;
            _alive = false;
            listener = _listener;
            _listener = null;
            conns = _connections.ToArray();
            _connections.Clear();
        }

        foreach (var conn in conns)
            conn.Close(ApiLimits.CloseGoingAway);
        try
        {
            listener?.Dispose();
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogWarning($"WebSocket API listener dispose failed: {ex.Message}");
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

    void OnSocket(IWebSocketConnection socket)
    {
        var conn = new ApiConnection(this, socket);
        var bounded = socket as WebSocketConnection;
        if (bounded != null && bounded.Handler != null)
            bounded.Handler = new BoundedHandler(bounded.Handler, ApiLimits.MaxIncomingSocketBytes);

        var path = socket.ConnectionInfo?.Path ?? "";
        var q = path.IndexOf('?');
        if (q >= 0) path = path.Substring(0, q);

        socket.OnOpen = () =>
        {
            if (!string.Equals(path, ApiLimits.Path, StringComparison.Ordinal))
            {
                conn.Close(ApiLimits.ClosePolicyViolation);
                return;
            }

            lock (_gate)
            {
                if (!_alive || _connections.Count >= ApiLimits.MaxConnections)
                {
                    conn.Close(ApiLimits.CloseTryAgainLater);
                    return;
                }

                _connections.Add(conn);
            }
        };
        socket.OnBinary = _ => conn.Close(ApiLimits.CloseUnsupportedData);
        socket.OnMessage = message => OnMessage(conn, bounded, message);
        socket.OnClose = () =>
        {
            if (bounded?.Handler is BoundedHandler)
                ((BoundedHandler)bounded.Handler).Reset();
            conn.NotifyDisconnected();
        };
        socket.OnError = ex => LiveStreamAssist.Logger.LogWarning($"WebSocket API connection error: {ex.Message}");
        var prevPing = socket.OnPing;
        socket.OnPing = data =>
        {
            if (bounded?.Handler is BoundedHandler h) h.Reset();
            prevPing?.Invoke(data);
        };
    }

    void OnMessage(ApiConnection conn, WebSocketConnection raw, string message)
    {
        if (raw?.Handler is BoundedHandler bounded)
            bounded.Reset();
        bool alive;
        lock (_gate) alive = _alive;
        if (!alive || !conn.IsOpen) return;
        if (message == null || Encoding.UTF8.GetByteCount(message) > ApiLimits.MaxRequestBytes)
        {
            conn.Close(ApiLimits.CloseMessageTooBig);
            return;
        }

        if (!ApiProtocol.TryParseMessage(message, out var request, out var parseError, out var id))
        {
            conn.EnqueueError(id, parseError);
            return;
        }

        var pending = conn.TryAdmit(request.Id, out var duplicate, out var busy);
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
                    conn.EnqueueError(request.Id, ApiErrors.ServerBusy());
                else
                    conn.Close(ApiLimits.CloseTryAgainLater);
            }

            return;
        }
        try
        {
            if (!ApiProtocol.IsSystemMethod(request.Method) && !ApiProtocol.IsDataMethod(request.Method))
            {
                pending.Complete(ApiErrors.MethodNotFound());
                return;
            }

            if (ApiProtocol.IsSystemMethod(request.Method))
            {
                var handled = ApiProtocol.HandleSystem(request.Method, request.Params, _version(), _modVersion);
                if (handled is ApiError sysErr)
                    pending.Complete(sysErr);
                else
                    pending.CompleteResult(handled);
                return;
            }

            if (!ApiProtocol.TryParseDataCall(request, out var call, out var callErr))
            {
                pending.Complete(callErr);
                return;
            }

            if (call.Method != DataMethod.Roots && !WebSocketApiFeature.IsKnownRoot(call.Root))
            {
                pending.Complete(ApiErrors.RootNotFound(call.Root));
                return;
            }

            var session = _session();
            var work = new GameWork
            {
                Pending = pending,
                Call = call,
                Generation = session.Generation,
                DeadlineMs = NowMs + ApiLimits.RequestTimeoutMs
            };
            if (!_dispatcher.TryEnqueue(work))
                pending.Complete(ApiErrors.RequestTimeout());
        }
        catch (Exception ex)
        {
            LiveStreamAssist.Logger.LogError($"API request handling failed: {ex}");
            pending.Complete(ApiErrors.InternalError());
        }
    }
}

sealed class BoundedHandler : IHandler
{
    readonly IHandler _inner;
    readonly int _max;
    int _consumed;

    public BoundedHandler(IHandler inner, int max)
    {
        _inner = inner;
        _max = max;
    }

    public void Reset() => _consumed = 0;

    public byte[] CreateHandshake(string subProtocol = null) => _inner.CreateHandshake(subProtocol);

    public void Receive(IEnumerable<byte> data)
    {
        var n = 0;
        foreach (var _ in data)
            n++;
        if (_consumed > _max - n)
            throw new WebSocketException(WebSocketStatusCodes.MessageTooBig);
        _consumed += n;
        _inner.Receive(data);
    }

    public byte[] FrameText(string text) => _inner.FrameText(text);
    public byte[] FrameBinary(byte[] bytes) => _inner.FrameBinary(bytes);
    public byte[] FramePing(byte[] bytes) => _inner.FramePing(bytes);
    public byte[] FramePong(byte[] bytes) => _inner.FramePong(bytes);
    public byte[] FrameClose(int code) => _inner.FrameClose(code);
}
