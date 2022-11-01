using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot;
using DiscordBot.Methods;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace TestingApp.WebSocket_Tests
{
    public class TestingServer
    {
        public static async Task Start()
        {
            var options = new WebSocketListenerOptions();
            options.Standards.RegisterRfc6455();
            options.Logger = new DebugLogger();
            options.PingMode = PingMode.LatencyControl;
            options.PingTimeout = new TimeSpan(0, 1, 0);
            var server = new WebSocketListener(new IPEndPoint(IPAddress.Any, 8002), options);
            await server.StartAsync();
            await Loop(server, CancellationToken.None);
        }

        private static async Task Loop(WebSocketListener server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
                try
                {
                    var ws = await server.AcceptWebSocketAsync(token).ConfigureAwait(false);
                    if (ws == null) continue;
                    var task = new Task(async () => await WebSocketServer.HandleConnectionAsync(ws));
                    task.Start();
                }
                catch (Exception е)
                {
                    await Debug.WriteAsync("Error Accepting clients: " + е.GetBaseException().Message);
                }
        }
    }
}