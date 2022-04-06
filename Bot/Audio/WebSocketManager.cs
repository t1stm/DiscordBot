using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using vtortola.WebSockets;

namespace BatToshoRESTApp.Audio
{
    public class WebSocketManager
    {
        public WebSocketManager(Queue queue, Player player)
        {
            Queue = queue;
            Player = player;
        }

        private List<WebSocket> WebSockets { get; } = new();
        private Queue Queue { get; }
        private Player Player { get; }

        public async Task OnWrite(WebSocket ws, string message)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Web Socket Message is \"{message}\"");
            var command = message.Split(':');
            if (command.Length != 2)
            {
                await Send(ws, $"Invalid command: \"{message}\"");
                return;
            }
            switch (command[0].ToLower())
            {
                case "queue":
                    await SendQueue(ws);
                    return;
                case "info":
                    await SendCurrentItem(ws);
                    return;
                case "search":
                    return;
                case "skip" when string.IsNullOrEmpty(command[1]):
                    await Player.Skip();
                    return;
                case "skip":
                    if (int.TryParse(command[1], out var times))
                        await Player.Skip(times);
                    else await Player.Skip();
                    return;
                case "move":
                    return;
                case "pause":
                    Player.Pause();
                    return;
                case "shuffle":
                    Player.Shuffle();
                    return;
                case "play":
                    return;
                case "playnext":
                    return;
                case "goto":
                    if (int.TryParse(command[1], out var place))
                        Player.GoToIndex(place);
                    return;
                case "remove":
                    if (int.TryParse(command[1], out var index))
                        await Player.RemoveFromQueue(index);
                    return;
                case "loop":
                    Player.ToggleLoop();
                    return;
                case "leave":
                    await Player.DisconnectAsync();
                    return;
            }
            await Send(ws, $"Invalid command: \"{message}\"");
        }

        public void Add(WebSocket ws)
        {
            try
            {
                lock (WebSockets)
                {
                    WebSockets.Add(ws);
                }

                var task = new Task(async () =>
                {
                    await Task.Delay(500);
                    await SendStarterData(ws);
                });
                task.Start();
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        public async Task Remove(WebSocket ws)
        {
            try
            {
                lock (WebSockets)
                {
                    if (!WebSockets.Contains(ws)) return;
                    WebSockets.Remove(ws);
                }

                await ws.CloseAsync();
                ws.Dispose();
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        private async Task SendStarterData(WebSocket ws)
        {
            await SendQueue(ws);
            await SendCurrentItem(ws);
        }

        public async Task BroadcastCurrentTime()
        {
            try
            {
                if (Player.Paused) return;
                await Broadcast($"Time:{Player.Stopwatch.ElapsedMilliseconds}");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Web Socket - Broadcasting Time Failed: {e}");
            }
        }

        private async Task SendQueue(WebSocket ws)
        {
            try
            {
                Queue queue;
                lock (Queue)
                {
                    queue = Queue;
                }

                await ws.WriteStringAsync($"Queue:{SerializeQueue(queue)}");
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        public async Task BroadcastQueue()
        {
            try
            {
                Queue queue;
                lock (Queue)
                {
                    queue = Queue;
                }

                await Broadcast($"Queue:{SerializeQueue(queue)}");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Web Socket - Broadcasting Queue failed: \"{e}\"");
            }
        }

        public async Task BroadcastCurrentItem()
        {
            try
            {
                await Broadcast($"Item:{SerializeCurrent()}");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Web Socket - Broadcasting Item failed: \"{e}\"");
            }
        }

        private async Task SendCurrentItem(WebSocket ws)
        {
            await Send(ws, $"Item:{SerializeCurrent()}");
        }

        private async Task Broadcast(string message)
        {
            List<WebSocket> webSockets;
            lock (WebSockets)
            {
                webSockets = WebSockets;
            }
            foreach (var sock in webSockets)
                try
                {
                    if (sock.IsConnected)
                    {
                        await sock.WriteStringAsync(message);
                    }
                    else
                    {
                        var task = new Task(async () => { await Remove(sock); });
                        task.Start();
                    }
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("close")) return;
                    var task = new Task(async () => { await Remove(sock); });
                    task.Start();
                }
        }
        
        private string SerializeCurrent()
        {
            return JsonSerializer.Serialize(Player.ToPlayerInfo());
        }

        private static async Task Send(WebSocket ws, string message)
        {
            await ws.WriteStringAsync(message);
        }

        private static string SerializeIPlayableItem(PlayableItem item)
        {
            return JsonSerializer.Serialize(item.ToSearchResult());
        }

        private static string SerializeQueue(Queue queue)
        {
            var list = new List<BatTosho.SearchResult>();
            for (var index = 0; index < queue.Items.Count; index++)
            {
                var item = queue.Items[index];
                var listItem = item.ToSearchResult();
                listItem.Index = index;
                list.Add(listItem);
            }
            return JsonSerializer.Serialize(list);
        }
    }
}