using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiveStreamAssist.Api;
using Newtonsoft.Json.Linq;

namespace WebSocketTransportCheck;

static partial class ProductionChecks
{
    static int _failed;
    [ThreadStatic] static bool _producing;

    public static async Task<int> Run()
    {
        global::LiveStreamAssist.LiveStreamAssist.Logger.LogEvent += (_, entry) => Console.WriteLine(entry.Data);
        await DisconnectWakesSender();
        await QueueOverflowReleasesAdmission();
        await StopBeforeStart();
        NullParameters();
        ProductionStatisticsRefresh();
        await ActiveSendRetainsAdmission();
        await SendDeadlineIncludesQueueTime();
        await ExecutingWorkTimesOut();
        await SessionCancellationKeepsNewWork();
        await PhysicalQueueRemainsBounded();
        await OversizedResult();
        await NetworkLifetime();
        await StartupRaces();
        Console.WriteLine(_failed == 0 ? "OK production checks" : $"FAILED {_failed} production checks");
        return _failed == 0 ? 0 : 1;
    }

    static WebSocketApiServer CreateServer() => new WebSocketApiServer(IPAddress.Loopback, 0, "test",
        () => GameVersionSnapshot.Empty, () => new SessionSnapshot(0, null), null);

    static async Task DisconnectWakesSender()
    {
        var server = CreateServer();
        using var connection = new ApiConnection(server);
        using (var socket = new TestSocket())
        using (var cancel = new CancellationTokenSource())
        {
            connection.Attach(socket);
            var sending = connection.RunSendLoop(socket, cancel.Token);
            connection.NotifyDisconnected();
            Check(await Task.WhenAny(sending, Task.Delay(500)) == sending, "disconnect wakes idle sender");
            cancel.Cancel();
            await sending;
        }
        await server.StopAsync();
    }

    static async Task QueueOverflowReleasesAdmission()
    {
        var server = CreateServer();
        server.Start();
        using var connection = new ApiConnection(server);
        using (var socket = new TestSocket())
        {
            connection.Attach(socket);
            for (var i = 0; i < ApiLimits.MaxPendingRequestsPerConnection; i++)
                Check(connection.TryAdmit("held-" + i, out _, out _) != null, "admit held request " + i);
            for (var i = 0; i <= ApiLimits.MaxPendingRequestsPerConnection; i++)
                connection.EnqueueError(null, ApiErrors.ParseError());
            connection.NotifyDisconnected();
            var available = 0;
            while (server.TryIncrementPending()) available++;
            Check(available == ApiLimits.MaxPendingRequests, "overflow releases every global admission slot");
            while (available-- > 0) server.DecrementPending();
            await connection.CloseCompletion;
        }
        await server.StopAsync();
    }

    static async Task StopBeforeStart()
    {
        var server = CreateServer();
        await server.StopAsync();
        server.Start();
        var admitted = server.TryIncrementPending();
        Check(!admitted, "stopped owner cannot start a listener later");
        if (admitted) server.DecrementPending();
        await server.StopAsync();
    }

    static void NullParameters()
    {
        var parsed = ApiProtocol.TryParseMessage(
            "{\"jsonrpc\":\"2.0\",\"id\":\"null\",\"method\":\"system.ping\",\"params\":null}",
            out _, out var error, out _);
        Check(!parsed && error?.Kind == "INVALID_PARAMS", "explicit null params is not omission");
        foreach (var name in new[] { "path", "select", "offset", "limit" })
        {
            var request = new ApiRequest
            {
                Method = ApiProtocol.MethodRead,
                Params = new JObject { ["root"] = "history", [name] = JValue.CreateNull() }
            };
            Check(!ApiProtocol.TryParseDataCall(request, out _, out error) && error.Kind == "INVALID_PARAMS",
                "explicit null " + name + " is rejected");
        }
    }

    static async Task ActiveSendRetainsAdmission()
    {
        var server = CreateServer();
        await server.StartAsync();
        using var connection = new ApiConnection(server);
        using var socket = new TestSocket();
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        socket.Sending = _ => release.Task;
        connection.Attach(socket);
        var sending = connection.RunSendLoop(socket, CancellationToken.None);
        try
        {
            var pending = connection.TryAdmit("held", out _, out _);
            _producing = true;
            try { pending.TryCompleteResult(new SerializationProbe()); }
            finally { _producing = false; }
            await Bounded(socket.SendStarted.Task);
            Check(JObject.Parse(Encoding.UTF8.GetString(socket.Payload))["result"]?["Value"]?.Value<int>() == 42,
                "serialization does not run inline in the producer");
            Check(connection.TryAdmit("held", out var duplicate, out _) == null && duplicate,
                "active send retains the request ID");
            for (var i = 0; i < ApiLimits.MaxPendingRequestsPerConnection - 1; i++)
                connection.EnqueueError(null, ApiErrors.ParseError());
            Check(!connection.CanSendBusy(), "active send counts against the bounded output queue");
            connection.EnqueueError(null, ApiErrors.ParseError());
            Check(!connection.IsOpen, "output overflow starts connection closure");
        }
        finally
        {
            release.TrySetResult(true);
            connection.NotifyDisconnected();
            await Bounded(sending);
            await connection.CloseCompletion;
            await server.StopAsync();
        }
    }

    static async Task SendDeadlineIncludesQueueTime()
    {
        var server = CreateServer();
        using var connection = new ApiConnection(server);
        using var socket = new TestSocket();
        var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        socket.Sending = token =>
        {
            if (Interlocked.Increment(ref calls) == 1) return first.Task;
            second.TrySetResult(true);
            return Task.Delay(Timeout.Infinite, token);
        };
        connection.Attach(socket);
        var sending = connection.RunSendLoop(socket, CancellationToken.None);
        try
        {
            connection.EnqueueError(null, ApiErrors.ParseError());
            await Bounded(socket.SendStarted.Task);
            connection.EnqueueError(null, ApiErrors.ParseError());
            await Task.Delay(ApiLimits.RequestTimeoutMs - 750);
            first.TrySetResult(true);
            await Bounded(second.Task);
            Check(await Task.WhenAny(sending, Task.Delay(2000)) == sending,
                "queued time is deducted from the active-send deadline");
        }
        finally
        {
            first.TrySetResult(true);
            connection.NotifyDisconnected();
            await Bounded(sending);
            await server.StopAsync();
        }
    }

    static async Task ExecutingWorkTimesOut()
    {
        var server = CreateServer();
        using var connection = new ApiConnection(server);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var context = new QueuedContext();
        long now = 0;
        var dispatcher = new MainThreadDispatcher(context, new object(), _ =>
        {
            entered.Set();
            if (!release.Wait(5000)) throw new TimeoutException("Held executor was not released.");
            return "late result";
        }, () => Interlocked.Read(ref now), () => 1);
        var pending = new PendingRequest { Connection = connection, Id = "running" };
        dispatcher.TryEnqueue(new GameWork { Pending = pending, DeadlineMs = 10 });
        var execution = Task.Run(context.RunOne);
        try
        {
            Check(entered.Wait(2000), "game-work executor entered");
            Interlocked.Exchange(ref now, 10);
            var completed = await Task.WhenAny(pending.Completion, Task.Delay(1500)) == pending.Completion;
            Check(completed && pending.Completion.Result.Error?.Kind == "REQUEST_TIMEOUT",
                "watchdog completes a request while its executor is blocked");
        }
        finally
        {
            release.Set();
            await Bounded(execution);
            dispatcher.Stop();
            await server.StopAsync();
        }
        Check(pending.Completion.Result.Error?.Kind == "REQUEST_TIMEOUT", "late executor result cannot replace timeout");
    }

    static async Task SessionCancellationKeepsNewWork()
    {
        var server = CreateServer();
        using var connection = new ApiConnection(server);
        var context = new QueuedContext();
        var dispatcher = new MainThreadDispatcher(context, new object(), _ => "current", () => 0, () => 1);
        try
        {
            var old = new PendingRequest { Connection = connection };
            var current = new PendingRequest { Connection = connection };
            dispatcher.TryEnqueue(new GameWork { Pending = old, Generation = 1, DeadlineMs = 10 });
            dispatcher.TryEnqueue(new GameWork { Pending = current, Generation = 2, DeadlineMs = 10 });
            dispatcher.CancelQueued(work => work.Generation == 2 ? null : ApiErrors.SessionChanged());
            context.RunOne();
            Check(old.Completion.Result.Error?.Kind == "SESSION_CHANGED" && Equals(current.Completion.Result.Result, "current"),
                "session invalidation preserves work admitted into the new session");
        }
        finally
        {
            dispatcher.Stop();
            await server.StopAsync();
        }
    }

    static async Task PhysicalQueueRemainsBounded()
    {
        var server = CreateServer();
        using var connection = new ApiConnection(server);
        var dispatcher = new MainThreadDispatcher(new QueuedContext(), new object(), _ => null, () => 0, () => 1);
        try
        {
            for (var i = 0; i < ApiLimits.MaxPendingRequests; i++)
                if (!dispatcher.TryEnqueue(new GameWork { Pending = new PendingRequest { Connection = connection }, DeadlineMs = 1 }))
                    throw new InvalidOperationException("Physical queue rejected work before its limit.");
            Check(!dispatcher.TryEnqueue(new GameWork()), "physical game-work queue rejects overflow");
            connection.NotifyDisconnected();
            await Task.Delay(750);
            using var next = new ApiConnection(server);
            Check(dispatcher.TryEnqueue(new GameWork { Pending = new PendingRequest { Connection = next }, DeadlineMs = 1 }),
                "watchdog reclaims canceled physical queue entries without pumping Unity");
        }
        finally
        {
            dispatcher.Stop();
            await server.StopAsync();
        }
    }

    static async Task OversizedResult()
    {
        var server = CreateServer();
        await server.StartAsync();
        using var connection = new ApiConnection(server);
        using var socket = new TestSocket();
        connection.Attach(socket);
        var sending = connection.RunSendLoop(socket, CancellationToken.None);
        try
        {
            connection.TryAdmit("large", out _, out _).TryCompleteResult(new string('x', ApiLimits.MaxResponseBytes));
            await Bounded(socket.SendStarted.Task);
            var response = JObject.Parse(Encoding.UTF8.GetString(socket.Payload));
            Check(response["error"]?["data"]?["kind"]?.Value<string>() == "LIMIT_EXCEEDED",
                "bounded serialization returns LIMIT_EXCEEDED");
        }
        finally
        {
            connection.NotifyDisconnected();
            await Bounded(sending);
            await connection.CloseCompletion;
            await server.StopAsync();
        }
    }

    static async Task NetworkLifetime()
    {
        var port = FreePort();
        WebSocketApiServer server = null;
        var dispatcher = new MainThreadDispatcher(new QueuedContext(), new object(), _ => ApiErrors.GameNotReady(),
            () => server.NowMs, () => 1);
        server = new WebSocketApiServer(IPAddress.Loopback, port, "test", () => GameVersionSnapshot.Empty,
            () => new SessionSnapshot(0, null), dispatcher);
        var uri = new Uri("ws://127.0.0.1:" + port + ApiLimits.Path);
        await server.StartAsync();
        try
        {
            using (var http = new HttpClient())
            using (var cancel = new CancellationTokenSource(5000))
            using (var response = await http.GetAsync("http://127.0.0.1:" + port + "/nope", cancel.Token))
                Check(response.StatusCode == HttpStatusCode.NotFound, "wrong route is rejected before upgrade");
            using (var client = await Connect(uri))
            {
                await Send(client, "{\"jsonrpc\":\"2.0\",\"id\":\"fragment\",", false);
                await Send(client, "\"method\":\"system.ping\"}");
                var response = await ReceiveJson(client);
                Check(response["id"]?.Value<string>() == "fragment" && response["result"]?["serverTimeUtc"] != null,
                    "production transport reassembles fragmented JSON");
                await Close(client);
            }
            using (var client = await Connect(uri))
            {
                using var cancel = new CancellationTokenSource(5000);
                await client.SendAsync(new ArraySegment<byte>(new byte[] { 1 }), WebSocketMessageType.Binary, true, cancel.Token);
                await ExpectClose(client, ApiLimits.CloseUnsupportedData);
            }
            using (var client = await Connect(uri))
            {
                using var cancel = new CancellationTokenSource(5000);
                await client.SendAsync(new ArraySegment<byte>(new byte[] { 0xff }), WebSocketMessageType.Text, true, cancel.Token);
                await ExpectClose(client, ApiLimits.CloseInvalidPayload);
            }
            using (var client = await Connect(uri))
            {
                await Send(client, new string('x', ApiLimits.MaxRequestBytes + 1));
                await ExpectClose(client, ApiLimits.CloseMessageTooBig);
            }
            using (var client = await Connect(uri))
            {
                await Send(client, "{\"jsonrpc\":\"2.0\",\"id\":\"held\",\"method\":\"data.roots\"}");
                await Send(client, "{\"jsonrpc\":\"2.0\",\"id\":\"held\",\"method\":\"system.ping\",\"params\":[]}");
                await ExpectClose(client, ApiLimits.ClosePolicyViolation);
            }
            using (var client = await Connect(uri))
            {
                await Send(client, "{\"jsonrpc\":\"2.0\",\"id\":\"blocked\",\"method\":\"data.roots\"}");
                await Send(client, "{\"jsonrpc\":\"2.0\",\"id\":\"ping\",\"method\":\"system.ping\"}");
                Check((await ReceiveJson(client))["id"]?.Value<string>() == "ping", "system ping bypasses blocked game work");
                var timeout = await ReceiveJson(client);
                Check(timeout["id"]?.Value<string>() == "blocked" &&
                      timeout["error"]?["data"]?["kind"]?.Value<string>() == "REQUEST_TIMEOUT",
                    "blocked pump returns a network timeout response");
                await Close(client);
            }
            using (var client = await Connect(uri))
            using (var cancel = new CancellationTokenSource(ApiLimits.RequestTimeoutMs + 2500))
            {
                await Send(client, "", false);
                await Send(client, "{", false);
                var closed = false;
                try
                {
                    var result = await client.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), cancel.Token);
                    closed = result.MessageType == WebSocketMessageType.Close;
                }
                catch (WebSocketException) { closed = !cancel.IsCancellationRequested; }
                catch (OperationCanceledException) { }
                Check(closed, "stalled fragmented message expires without another receive completing");
            }
            using (var client = await Connect(uri))
            {
                var stopped = server.StopAsync();
                await ExpectClose(client, ApiLimits.CloseGoingAway);
                await Bounded(stopped, 7000);
            }
        }
        finally
        {
            dispatcher.Stop();
            await server.StopAsync();
        }
        var rebound = new TcpListener(IPAddress.Loopback, port);
        rebound.Start();
        rebound.Stop();
        Check(true, "shutdown releases the listener port");
    }

    static async Task StartupRaces()
    {
        for (var i = 0; i < 4; i++)
        {
            var server = CreateServer();
            var startup = server.StartAsync();
            var stopped = server.StopAsync();
            try { await startup; }
            catch (OperationCanceledException) { }
            await Bounded(stopped, 7000);
            Check(!server.TryIncrementPending(), "stop during startup leaves no live owner " + i);
        }
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var occupied = new WebSocketApiServer(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port, "test",
            () => GameVersionSnapshot.Empty, () => new SessionSnapshot(0, null), null);
        try
        {
            try
            {
                await occupied.StartAsync();
                Check(false, "occupied port rejects startup");
            }
            catch (IOException) { Check(true, "occupied port rejects startup"); }
            await Bounded(occupied.StopAsync(), 7000);
        }
        finally { listener.Stop(); }
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    static async Task Bounded(Task task, int milliseconds = 2000)
    {
        if (await Task.WhenAny(task, Task.Delay(milliseconds)) != task)
            throw new TimeoutException("Production check did not complete within its deadline.");
        await task;
    }

    static async Task<ClientWebSocket> Connect(Uri uri)
    {
        var client = new ClientWebSocket();
        using var cancel = new CancellationTokenSource(5000);
        try { await client.ConnectAsync(uri, cancel.Token); }
        catch { client.Dispose(); throw; }
        return client;
    }

    static async Task Send(ClientWebSocket client, string text, bool end = true)
    {
        using var cancel = new CancellationTokenSource(5000);
        await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, end, cancel.Token);
    }

    static async Task<JObject> ReceiveJson(ClientWebSocket client)
    {
        using var cancel = new CancellationTokenSource(7000);
        var buffer = new byte[ApiLimits.MaxResponseBytes];
        var used = 0;
        WebSocketReceiveResult result;
        do
        {
            if (used == buffer.Length) throw new InvalidDataException("Response exceeded its limit.");
            result = await client.ReceiveAsync(new ArraySegment<byte>(buffer, used, buffer.Length - used), cancel.Token);
            if (result.MessageType != WebSocketMessageType.Text) throw new InvalidDataException("Expected a JSON response.");
            used += result.Count;
        } while (!result.EndOfMessage);
        return JObject.Parse(Encoding.UTF8.GetString(buffer, 0, used));
    }

    static async Task ExpectClose(ClientWebSocket client, int code)
    {
        using var cancel = new CancellationTokenSource(7000);
        var result = await client.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), cancel.Token);
        Check(result.MessageType == WebSocketMessageType.Close && (int?)result.CloseStatus == code, "WebSocket close " + code);
        await Close(client);
    }

    static async Task Close(ClientWebSocket client)
    {
        using var cancel = new CancellationTokenSource(5000);
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancel.Token);
    }

    static void Check(bool condition, string name)
    {
        Console.WriteLine((condition ? "ok  " : "fail ") + name);
        if (!condition) _failed++;
    }

    sealed class TestSocket : WebSocket
    {
        WebSocketState _state = WebSocketState.Open;
        public byte[] Payload;
        public readonly TaskCompletionSource<bool> SendStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public Func<CancellationToken, Task> Sending = _ => Task.CompletedTask;
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string SubProtocol => null;
        public override void Abort() => _state = WebSocketState.Aborted;
        public override void Dispose() => _state = WebSocketState.Closed;
        public override Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken token)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }
        public override Task CloseOutputAsync(WebSocketCloseStatus status, string description, CancellationToken token)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token) =>
            throw new NotSupportedException();
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType type, bool end, CancellationToken token)
        {
            Payload = new byte[buffer.Count];
            Buffer.BlockCopy(buffer.Array, buffer.Offset, Payload, 0, buffer.Count);
            SendStarted.TrySetResult(true);
            return Sending(token);
        }
    }

    sealed class SerializationProbe
    {
        public int Value => _producing ? throw new InvalidOperationException("Inline serialization") : 42;
    }

    sealed class QueuedContext : SynchronizationContext
    {
        readonly Queue<Action> _work = new Queue<Action>();
        public override void Post(SendOrPostCallback callback, object state)
        {
            lock (_work) _work.Enqueue(() => callback(state));
        }
        public void RunOne()
        {
            Action work;
            lock (_work) work = _work.Dequeue();
            work();
        }
    }
}
