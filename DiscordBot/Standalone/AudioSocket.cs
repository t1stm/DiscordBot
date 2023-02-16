#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms;
using DiscordBot.Objects;
using DiscordBot.Tools;
using vtortola.WebSockets;
using Debug = DiscordBot.Methods.Debug;
using Timer = System.Timers.Timer;

namespace DiscordBot.Standalone
{
    public class AudioSocket
    {
        private readonly List<EncodedAudio> Audios = new();
        private readonly Timer ReadyTimer = new();
        private readonly Timer Timer = new();
        private Settings Options;
        private List<SearchResult> Queue = Enumerable.Empty<SearchResult>().ToList();
        private ulong _clientIndex;

        public AudioSocket()
        {
            Options = new Settings
            {
                AllowNonAdminControl = true,
                AllowAnonymousJoining = true
            };
            Timer.Elapsed += TimerOnElapsed;
            Timer.Interval = 10 * 1000; // Ten seconds
            Timer.Start();
            ReadyTimer.Elapsed += ReadyTimerOnElapsed;
            ReadyTimer.Interval = 50; // Miliseconds
            ReadyTimer.Start();
        }

        public Stopwatch InactiveStopwatch { get; } = new();

        public WebSocket? Admin { get; set; }
        public List<Client> Clients { get; } = new();
        public Guid SessionId { get; init; }
        private int Current { get; set; }
        private bool Paused { get; set; }

        private List<ChatMessage> Messages = new();

        private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            var now = DateTime.UtcNow.Ticks;
            lock (Audios)
            {
                Audios.RemoveAll(r => r.Expire >= now);
            }
        }

        private async void ReadyTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            List<Client> clients;
            lock (Clients)
            {
                clients = Clients.ToList();
            }

            var all = clients.AsParallel().All(r => r.Ready) && Clients.Count != 0;
            foreach (var client in clients) await client.Socket.SendPingAsync().ConfigureAwait(false);
            if (!all) return;

            ClearReady();
            await Broadcast("Play:").ConfigureAwait(false);
            await Debug.WriteAsync("Ready: Broadcasting play message.");
        }

        private void ClearReady()
        {
            lock (Clients)
            {
                foreach (var client in Clients) client.Ready = false;
            }
        }

        private async Task OnWrite(Client client, string message)
        {
            try
            {
                var split = message.Split(':');
                if (split.Length < 2)
                {
                    await Respond(client.Socket, "Invalid syntax");
                    await Debug.WriteAsync("AudioSocket made an invalid request.");
                    return;
                }

                var joined = string.Join(':', split[1..]);
                var command = split[0].ToLower();
                if (Bot.DebugMode)
                    await Debug.WriteAsync($"AudioSocket OnWrite message is: \"{message}\"");

                bool all;
                switch (command)
                {
                    case "get":
                        await Debug.WriteAsync($"Audio WebSocket Get: \'{joined}\'");
                        var search = await Search.Get(joined);
                        var first = search?.First();
                        if (first == null)
                        {
                            await Respond(client.Socket, "Get:Not Found");
                            return;
                        }

                        var addUrl = first.GetAddUrl();
                        EncodedAudio? existing;

                        lock (Audios)
                        {
                            existing = Audios.AsParallel().FirstOrDefault(r => r.AddUrl == addUrl);
                        }

                        if (existing is null)
                        {
                            StreamSpreader streamSpreader;
                            lock (Audios)
                            {
                                streamSpreader = new StreamSpreader(CancellationToken.None)
                                {
                                    KeepCached = true
                                };
                                Audios.Add(existing = new EncodedAudio
                                {
                                    Spreader = streamSpreader,
                                    AddUrl = addUrl
                                });
                            }

                            var task = new Task(async () =>
                            {
                                var ffmpeg = new FfMpeg2();
                                await streamSpreader.ReadStream(ffmpeg.Convert(first, codec: "-c:a libopus",
                                    addParameters: $"-b:a {96}k"));
                            });
                            task.Start();
                        }

                        var stream = new MemoryStream();
                        existing.Spreader.AddDestination(stream);
                        var buffer =
                            new byte[1 <<
                                     13]; // I can't go any higher because the javascript decoder goes mad. i know, it's a dumb reason
                        while (await stream.ReadAsync(buffer) != 0)
                        {
                            var obj = new
                            {
                                forId = addUrl,
                                data = Convert.ToBase64String(buffer)
                            };
                            var dest = client.Socket.CreateMessageWriter(WebSocketMessageType.Text);
                            //await new StreamWriter(dest).WriteAsync("Data:");
                            await JsonSerializer.SerializeAsync(dest, obj);
                            await dest.FlushAsync();
                            await dest.CloseAsync();
                        }

                        await stream.DisposeAsync();
                        return;

                    case "current":
                        await Respond(client.Socket, $"Current:{Current}").ConfigureAwait(false);
                        return;

                    case "end":
                        client.Ended = true;
                        lock (Clients)
                        {
                            all = Clients.AsParallel().All(r => r.Ended);
                        }

                        if (!all || Current + 1 == Queue.Count) return;

                        ClearReady();
                        await Broadcast("Skip:").ConfigureAwait(false);

                        return;

                    case "ready":
                        await Debug.WriteAsync("A client is ready.");
                        client.Ready = true;
                        return;
                }

                if (client.Socket != Admin && !Options.AllowNonAdminControl)
                {
                    await Respond(client.Socket, "Invalid permission").ConfigureAwait(false);
                    await Debug.WriteAsync("AudioSocket made an unauthorized request.");
                    return;
                }

                switch (command)
                {
                    case "pause":
                        Paused = !Paused;
                        await Broadcast($"Pause:{Paused}").ConfigureAwait(false);
                        return;

                    case "queue":
                        lock (Queue)
                        {
                            Queue = JsonSerializer.Deserialize<List<SearchResult>>(joined) ??
                                    Enumerable.Empty<SearchResult>().ToList();
                        }

                        await Broadcast($"Queue:{JsonSerializer.Serialize(Queue)}", client);
                        lock (Clients)
                        {
                            all = Clients.AsParallel().All(r => r.Ended);
                        }

                        if (!all || Current + 1 == Queue.Count) return;
                        await Broadcast("Skip:");
                        return;
                    
                    case "chat":
                        var chatMessage = new ChatMessage
                        {
                            User = client.Name,
                            Message = joined,
                            SendTime = DateTime.UtcNow
                        };
                        lock (Messages) {
                            Messages.Add(chatMessage);
                        }
                        await Broadcast($"Chat:{JsonSerializer.Serialize(chatMessage)}", client).ConfigureAwait(false);
                        return;
                    
                    case "seek":
                        if (!int.TryParse(joined, out var position))
                        {
                            await Respond(client.Socket, "Fail:Unable to parse position.");
                            return;
                        }
                        await Broadcast($"Seek:{position}").ConfigureAwait(false);
                        
                        return;

                    case "back":
                        if (Current - 1 < 0)
                        {
                            await Debug.WriteAsync(
                                $"Skip current: {Current} is = to Queue.Count {Queue.Count} when --.");
                            return;
                        }

                        Current--;
                        ClearReady();
                        await Broadcast("Back:").ConfigureAwait(false);
                        return;

                    case "skip":
                        if (Current + 1 == Queue.Count)
                        {
                            await Debug.WriteAsync(
                                $"Skip current: {Current} is = to Queue.Count {Queue.Count} when ++.");
                            return;
                        }

                        Current++;
                        ClearReady();
                        await Broadcast("Skip:").ConfigureAwait(false);
                        return;

                    case "goto":
                        var parsed = int.TryParse(joined, out var num);
                        if (!parsed)
                        {
                            await Respond(client.Socket, "Invalid number syntax").ConfigureAwait(false);
                            return;
                        }

                        if (Current == Queue.Count || Current < 0)
                        {
                            await Debug.WriteAsync($"GoTo number was out of range. {Current} - {Queue.Count}");
                            Current = 0;
                            return;
                        }

                        ClearReady();
                        await Broadcast($"GoTo:{num}").ConfigureAwait(false);
                        Current = num;
                        return;

                    case "stop":

                        return;

                    case "options" when client.Socket != Admin:
                        return;

                    case "options":
                        lock (Options)
                        {
                            Options = JsonSerializer.Deserialize<Settings>(joined) ?? new Settings();
                        }

                        return;

                    case "ban":
                        return;
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"AudioSocket OnWrite error: \"{e}\"");
            }
        }

        private async Task Broadcast(string message)
        {
            if (Bot.DebugMode)
                await Debug.WriteAsync($"AudioSocket Broadcasting message: \"{message}\"");
            List<Client> clients;

            lock (Clients)
            {
                clients = Clients.ToList();
            }

            var tasks = clients.Select(client => new Task(async () =>
            {
                try
                {
                    await Respond(client.Socket, message).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync(
                        $"Sending broadcast message to client: \"{(client.IsAnon ? "Anonymous" : client.Token)}\" failed. \"{e}\"");
                }
            })).ToList();
            Parallel.ForEach(tasks, task =>
            {
                task.Start();
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task Broadcast(string message, Client? exclude)
        {
            if (Bot.DebugMode)
                await Debug.WriteAsync($"AudioSocket Broadcasting message: \"{message}\"");
            List<Client> clients;

            lock (Clients)
            {
                clients = Clients.ToList();
            }

            if (exclude != null) clients.Remove(exclude);

            foreach (var client in clients)
                try
                {
                    await Respond(client.Socket, message).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync(
                        $"Sending broadcast message to client: \"{(client.IsAnon ? "Anonymous" : client.Token)}\" failed. \"{e}\"");
                }
        }

        private static async Task Respond(WebSocket socket, string message)
        {
            try
            {
                if (!socket.IsConnected)
                {
                    if (Bot.DebugMode)
                        await Debug.WriteAsync("AudioSocket respond tried to send message to not connected user.");
                    return;
                }

                if (Bot.DebugMode)
                    await Debug.WriteAsync($"AudioSocket Respond message: \"{message}\"");
                await socket.WriteStringAsync(message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Responding to audio socket failed: \"{e}\"");
            }
        }

        public async Task AddClient(WebSocket ws, string? suppliedToken)
        {
            try
            {
                var client = new Client
                {
                    Socket = ws,
                    Token = suppliedToken,
                    Index = _clientIndex++
                };
                if (client.Token != null)
                {
                    try
                    {
                        var user = await User.FromToken(client.Token);
                        if (user?.Id != null)
                        {
                            var discordUser = await Bot.Clients[0].GetUserAsync(user.Id);
                            client.Username = $"{discordUser.Username} #{discordUser.Discriminator}";
                        }
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Unable to get username when token isn't null: \"{e}\"");
                    }
                }
                lock (Clients)
                {
                    Clients.Add(client);
                }

                List<ChatMessage> messages;

                lock (Messages)
                {
                    messages = Messages.ToList();
                }
                
                await Respond(ws, $"Queue:{JsonSerializer.Serialize(Queue)}").ConfigureAwait(false);
                await Respond(ws, $"OldMessages:{JsonSerializer.Serialize(messages)}").ConfigureAwait(false);
                await Broadcast($"UserJoin:{client.Name}", client).ConfigureAwait(false);

                var task = new Task(async () =>
                {
                    while (ws.IsConnected)
                        try
                        {
                            var message = await ws.ReadMessageAsync(CancellationToken.None);
                            if (message == null)
                            {
                                lock (Clients)
                                {
                                    Clients.Remove(client);
                                }

                                if (Admin == ws) Admin = Clients.FirstOrDefault()?.Socket;
                                await ws.CloseAsync();
                                await Broadcast($"UserLeave:{client.Name}").ConfigureAwait(false);
                                return;
                            }

                            if (message.MessageType != WebSocketMessageType.Text)
                                continue;
                            string contents;
                            using (var reader = new StreamReader(message, new UTF8Encoding(false, false)))
                            {
                                contents = await reader.ReadToEndAsync();
                            }

                            await OnWrite(client, contents);
                        }
                        catch (Exception e)
                        {
                            await Debug.WriteAsync($"Exception reading AudioSocket message: \"{e}\"");
                        }
                });
                task.Start();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Client Session failed: \"{e}\"");
            }
        }

        public async Task SetAdmin(WebSocket ws, string? suppliedToken = null)
        {
            try
            {
                Admin = ws;
                await AddClient(ws, suppliedToken);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Set Admin failed: \"{e}\"");
            }
        }

        public class Client
        {
            public WebSocket Socket { get; init; } = null!;
            public string? Token { get; init; }
            public bool IsAnon => string.IsNullOrEmpty(Token) || Token is "null" or "undefined";
            public string Username = "";
            public string Name => IsAnon || string.IsNullOrEmpty(Username) ? $"Anonymous #{Index}" : Username;
            public ulong Index;
            public bool Ready { get; set; }
            public bool Ended { get; set; }
        }

        private class EncodedAudio
        {
            public string? AddUrl { get; init; }
            public StreamSpreader Spreader { get; init; } = null!;
            public long Expire { get; } = DateTime.UtcNow.AddMinutes(Audio.AudioCacheTimeout).Ticks;
        }

        public class Settings
        {
            public bool AllowNonAdminControl { get; set; }
            public bool AllowAnonymousJoining { get; set; }
        }
    }
}
