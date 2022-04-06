using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace BatToshoRESTApp
{
    public static class WebSocketServer
    {
        private const int ListeningPort = 8001;

        public static async Task Start()
        {
            var options = new WebSocketListenerOptions();
            options.Standards.RegisterRfc6455();
            var server = new WebSocketListener(new IPEndPoint(IPAddress.Any, ListeningPort), options);
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
                    var task = new Task(async () => await HandleConnectionAsync(ws));
                    task.Start();
                }
                catch (Exception е)
                {
                    await Debug.WriteAsync("Error Accepting clients: " + е.GetBaseException().Message);
                }
        }

        private static async Task HandleConnectionAsync(WebSocket ws)
        {
            try
            {
                var req = ws.HttpRequest.RequestUri.ToString();
                await Debug.WriteAsync($"WebSocket connection with request: {req}");
                if (req.StartsWith("/ToshoWS/")) req = req[9..];

                var split = req.Split('/');
                if (split.Length != 3)
                {
                    await Fail(ws, "Invalid request");
                    return;
                }

                var clientSecret = split[0];
                if (!BatTosho.WebUiUsers.ContainsValue(clientSecret))
                {
                    await Fail(ws, "Not a user");
                    return;
                }

                var guildId = split[1];
                var succ = ulong.TryParse(guildId, out var guildKey);
                if (!succ || Bot.Clients.All(cl => !cl.Guilds.ContainsKey(guildKey)))
                {
                    await Fail(ws, "Not an available guild");
                    return;
                }

                var channelId = split[2];
                var succ2 = ulong.TryParse(channelId, out var channelKey);
                if (!succ2 || Manager.Main.All(pl => pl.VoiceChannel.Id != channelKey))
                {
                    await Fail(ws, "Not an active voice channel");
                    return;
                }

                Player player;
                lock (Manager.Main)
                {
                    player = Manager.Main.FirstOrDefault(cl => cl.VoiceChannel.Id == channelKey);
                    if (player == null)
                    {
                        var t = new Task(async () => { await Fail(ws, "Error"); });
                        t.Start();
                        return;
                    }
                }

                player.WebSocketManager.Add(ws);

                while (ws.IsConnected)
                {
                    var message = await ws.ReadStringAsync(CancellationToken.None);
                    await player.WebSocketManager.OnWrite(ws, message);
                }

                var ta = new Task(async () => { await player.WebSocketManager.Remove(ws); });
                ta.Start();
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        private static async Task Fail(WebSocket ws, string info = "No information specified")
        {
            await ws.WriteStringAsync($"Fail: \"{info}\"");
            await ws.CloseAsync();
            ws.Dispose();
        }
    }
}