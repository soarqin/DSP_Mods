using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.WebSockets;

namespace WebSocketTransportCheck;

static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (Array.IndexOf(args, "--regression") >= 0)
            return ProductionChecks.Run().GetAwaiter().GetResult();
        var port = 18081;
        var echo = false;
        foreach (var arg in args)
        {
            if (arg == "--echo") echo = true;
            else if (arg.StartsWith("--port=", StringComparison.Ordinal) &&
                     int.TryParse(arg.Substring(7), out var p))
                port = p;
        }

        using var host = BuildHost(IPAddress.Loopback, port);
        Console.WriteLine("transport=" + typeof(SocketTransportFactory).Assembly.FullName);
        Console.WriteLine("transportLocation=" + typeof(SocketTransportFactory).Assembly.Location);
        Console.WriteLine("websockets=" + typeof(WebSocketMiddleware).Assembly.FullName);
        Console.WriteLine("websocketsLocation=" + typeof(WebSocketMiddleware).Assembly.Location);
        Console.WriteLine("kestrel=" + typeof(KestrelServer).Assembly.FullName);
        Console.WriteLine("kestrelLocation=" + typeof(KestrelServer).Assembly.Location);
        Console.WriteLine("listening=ws://127.0.0.1:" + port + "/echo");
        host.Start();
        if (!echo)
        {
            RunClient(port).GetAwaiter().GetResult();
            Console.WriteLine("OK");
            return 0;
        }

        Console.WriteLine("echo running; Ctrl+C to stop");
        using var exit = new ManualResetEvent(false);
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            exit.Set();
        };
        exit.WaitOne();
        return 0;
    }

    internal static IWebHost BuildHost(IPAddress address, int port)
    {
        return new WebHostBuilder()
            .UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
            .UseKestrel(o =>
            {
                o.Listen(address, port);
                o.AddServerHeader = false;
                o.Limits.MaxConcurrentConnections = 8;
                o.Limits.MaxConcurrentUpgradedConnections = 8;
                o.Limits.MaxRequestHeadersTotalSize = 16 * 1024;
                o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
                o.Limits.MaxRequestBodySize = 1024;
            })
            .UseSockets()
            .Configure(app =>
            {
                app.UseWebSockets(new WebSocketOptions { ReceiveBufferSize = 4096 });
                app.Run(Handle);
            })
            .Build();
    }

    static async Task Handle(HttpContext context)
    {
        if (!string.Equals(context.Request.Path.Value, "/echo", StringComparison.Ordinal))
        {
            context.Response.StatusCode = 404;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        using (var ws = await context.WebSockets.AcceptWebSocketAsync())
        {
            Console.WriteLine("websocketType=" + ws.GetType().FullName);
            Console.WriteLine("websocketAssembly=" + ws.GetType().Assembly.FullName);
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            var buffer = new byte[4096];
            var payload = new byte[65536];
            var used = 0;
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), lifetime.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    lifetime.CancelAfter(5000);
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", lifetime.Token);
                    return;
                }

                if (used + result.Count > payload.Length)
                {
                    lifetime.CancelAfter(5000);
                    await ws.CloseAsync(WebSocketCloseStatus.MessageTooBig, "", lifetime.Token);
                    return;
                }

                Buffer.BlockCopy(buffer, 0, payload, used, result.Count);
                used += result.Count;
                if (!result.EndOfMessage) continue;
                await ws.SendAsync(new ArraySegment<byte>(payload, 0, used), result.MessageType, true,
                    lifetime.Token);
                used = 0;
            }
        }
    }

    static async Task RunClient(int port)
    {
        using (var client = new ClientWebSocket())
        using (var timeout = new CancellationTokenSource(5000))
        {
            await client.ConnectAsync(new Uri("ws://127.0.0.1:" + port + "/echo"), timeout.Token);
            var payload = Encoding.UTF8.GetBytes("ping");
            await client.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, false,
                timeout.Token);
            await client.SendAsync(new ArraySegment<byte>(new byte[0]), WebSocketMessageType.Text, true,
                timeout.Token);
            var received = new byte[payload.Length];
            var used = 0;
            WebSocketReceiveResult result;
            do
            {
                result = await client.ReceiveAsync(new ArraySegment<byte>(received, used, received.Length - used), timeout.Token);
                if (result.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("Expected an echo response.");
                used += result.Count;
            } while (!result.EndOfMessage && used < received.Length);
            if (!result.EndOfMessage || used != payload.Length || Encoding.UTF8.GetString(received) != "ping")
                throw new InvalidOperationException("Echo response did not match the fragmented message.");
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
        }
    }
}
