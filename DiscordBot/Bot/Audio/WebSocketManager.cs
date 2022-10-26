using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms;
using DiscordBot.Data;
using DiscordBot.Data.Models;
using DiscordBot.Methods;
using DiscordBot.Objects;
using vtortola.WebSockets;

namespace DiscordBot.Audio
{
    public class WebSocketManager
    {
        public WebSocketManager(Queue queue, Player player)
        {
            Queue = queue;
            Player = player;
        }

        private Dictionary<WebSocket, string> WebSockets { get; } = new();
        private Queue Queue { get; }
        private Player Player { get; }

        public async Task OnWrite(WebSocket ws, string message)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Web Socket Message is \"{message}\"");
            var command = message.Split(':');
            if (command.Length < 2)
            {
                await Send(ws, $"Invalid command: \"{message}\"");
                return;
            }

            if (!WebSockets.ContainsKey(ws)) return;

            switch (command[0].ToLower())
            {
                case "queue":
                    await SendQueue(ws);
                    return;
                case "info":
                    await SendCurrentItem(ws);
                    return;
                case "search":
                    var results = await Search.Get(string.Join(':', command[1..]), returnAllResults: true);
                    await Send(ws, $"Search:{JsonSerializer.Serialize(results?.Select(r => r.ToSearchResult()))}");
                    return;
                case "set":
                    SettingsParser(WebSockets[ws], string.Join(":", command[1..]));
                    return;
            }

            if (!IsInChannel(WebSockets[ws]))
            {
                await Send(ws, "Fail:Not in voice channel");
                return;
            }

            switch (command[0].ToLower())
            {
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
                    var searchTerm = string.Join(':', command[1..]);
                    if (string.IsNullOrEmpty(searchTerm))
                    {
                        await Send(ws, "Fail:Empty Search Term");
                        return;
                    }

                    var search = await Search.Get(searchTerm);
                    if (search == null) return;
                    Queue.AddToQueue(search);
                    return;
                case "playnext":
                    var searchTerm2 = string.Join(':', command[1..]);
                    if (string.IsNullOrEmpty(searchTerm2))
                    {
                        await Send(ws, "Fail:Empty Search Term");
                        return;
                    }

                    var resulted = await Search.Get(searchTerm2);
                    if (resulted == null) return;
                    Queue.AddToQueueNext(resulted);
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
                case "volume":
                    if (double.TryParse(command[1], out var volume))
                    {
                        if (Player.UpdateVolume(volume)) return;
                        await Send(ws, "Fail:Volume out of range");
                        return;
                    }

                    await Send(ws, "Fail:Invalid volume string");
                    return;
                case "leave":
                    Player.Disconnect();
                    return;
            }

            await Send(ws, $"Invalid command: \"{message}\"");
        }

        public void Add(WebSocket ws, string token)
        {
            try
            {
                lock (WebSockets)
                {
                    WebSockets.Add(ws, token);
                }

                var task = new Task(async () =>
                {
                    try
                    {
                        await Task.Delay(500);
                        await SendStarterData(ws);
                        await SendSettings(ws, token);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Sending Started Data failed: \"{e}\"");
                    }
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
                    if (!WebSockets.ContainsKey(ws)) return;
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

        public async Task BroadcastUpdateItem(int index, SearchResult result)
        {
            try
            {
                var json = JsonSerializer.Serialize(result);
                await Broadcast($"Update:{index}:{json}");
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
            await Send(ws, $"Time:{Player.Stopwatch.ElapsedMilliseconds}");
        }

        public async Task BroadcastCurrentTime()
        {
            try
            {
                if (Player.Paused) return;
                await Broadcast($"Time:{Player.Stopwatch.ElapsedMilliseconds}");
                if (Bot.DebugMode)
                    await Debug.WriteAsync($"Broadcasting time to: {WebSockets.Count} web sockets.");
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

        public async Task SendDying()
        {
            try
            {
                await Broadcast("Goodbye:");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Sending death web socket failed: {e}");
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
                if (Bot.DebugMode)
                    await Debug.WriteAsync($"Broadcasting queue to: {WebSockets.Count} web sockets.");
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
                if (Bot.DebugMode)
                    await Debug.WriteAsync($"Broadcasting current item to: {WebSockets.Count} web sockets.");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Web Socket - Broadcasting Item failed: \"{e}\"");
            }
        }

        private async Task SendCurrentItem(WebSocket ws)
        {
            try
            {
                await Send(ws, $"Item:{SerializeCurrent()}");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Web Socket - Sending current item failed: \"{e}\"");
            }
        }

        private async Task Broadcast(string message)
        {
            Dictionary<WebSocket, string> webSockets;
            lock (WebSockets)
            {
                webSockets = WebSockets;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            // No exception threading fix, don't use foreach loop. Big brain.
            for (var i = 0; i < webSockets.Count; i++)
            {
                var sock = webSockets.Keys.ToList()[i];
                try
                {
                    if (sock.IsConnected)
                    {
                        await Send(sock, message);
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
        }

        private bool IsInChannel(string token)
        {
            var call = Databases.UserDatabase.ReadCopy().Select(r => new User(r)).ToList();
            return Player.VoiceUsers.Any(user => call.ContainsKey(user.Id) && call.GetValue(user.Id) == token);
        }

        private string SerializeCurrent()
        {
            return JsonSerializer.Serialize(Player.ToPlayerInfo());
        }

        private static async Task Send(WebSocket ws, string message)
        {
            await ws.WriteStringAsync(message);
        }

        private static async Task SendSettings(WebSocket ws, string token)
        {
            var user = await User.FromToken(token);
            if (user == null)
            {
                await Debug.WriteAsync("A user is null, despite being logged in the Web UI Sockets.", false,
                    Debug.DebugColor.Error);
                return;
            }

            await Send(ws, $"Settings:{JsonSerializer.Serialize(user.ToWebUISettings())}");
        }

        private static void SettingsParser(string token, string json)
        {
            var data = JsonSerializer.Deserialize<UsersModel>(json);
            var request = new UsersModel
            {
                Token = token
            };
            var readUser = Databases.UserDatabase.Read(request);
            if (readUser == null) return;
            
            readUser.Language = data?.Language ?? readUser.Language;
            readUser.UiScroll = data?.UiScroll ?? readUser.UiScroll;
            readUser.ForceUiScroll = data?.ForceUiScroll ?? readUser.ForceUiScroll;
            readUser.VerboseMessages = data?.VerboseMessages ?? readUser.VerboseMessages;
            readUser.LowSpec = data?.LowSpec ?? readUser.LowSpec;
            readUser.SetModified?.Invoke();
        }

        private static string SerializeQueue(Queue queue)
        {
            return JsonSerializer.Serialize(queue.Items.Select(r => r.ToSearchResult()).ToList(),
                new JsonSerializerOptions
                {
                    WriteIndented = false
                });
        }
    }
}