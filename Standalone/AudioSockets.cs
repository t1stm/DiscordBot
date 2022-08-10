using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Audio.Objects;
using DiscordBot.Methods;
using vtortola.WebSockets;

namespace DiscordBot.Standalone
{
    public class AudioSocket
    {
        public AudioSocket()
        {
            Options = new Settings
            {
                AllowNonAdminSkipping = false,
                AllowAnonymousJoining = true
            };
        }
        public WebSocket Admin { get; set; }
        public List<Client> Clients { get; } = new();
        public Guid SessionId { get; init; }
        private List<SearchResult> Queue = Enumerable.Empty<SearchResult>().ToList();
        private int Current { get; set; } = 0;
        private Settings Options;

        private void ClearReady()
        {
            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    client.Ready = false;
                }
            }
        }

        private async Task OnWrite(Client client, string message)
        {
            var split = message.Split(':');
            if (split.Length < 2)
            {
                await Respond(client.Socket, "Invalid syntax");
                return;
            }
            
            var joined = string.Join(':', split[1..]);
            var command = split[0].ToLower();

            bool all;
            switch (command)
            {
                case "info":
                    if (!int.TryParse(joined, out var i))
                    {
                        await Respond(client.Socket, "Invalid index");
                        return;
                    }
                    var el = Queue.ElementAtOrDefault(i);
                    
                    if (el == null)
                    {
                        await Respond(client.Socket, "Index not found");
                        return;
                    }

                    await Respond(client.Socket, $"Item:{JsonSerializer.Serialize(el)}");
                    return;
                
                case "end":
                    client.Ended = true;
                    lock (Clients)
                    {
                        all = Clients.AsParallel().All(r => r.Ended);
                    }

                    if (!all || Current + 1 == Queue.Count) return;
                    
                    ClearReady();
                    await Broadcast("Skip:");

                    return;
                
                case "ready":
                    client.Ready = true;
                    lock (Clients)
                    {
                        all = Clients.AsParallel().All(r => r.Ready);
                    }
                    if (!all || Current + 1 == Queue.Count) return;
                    
                    ClearReady();
                    await Broadcast("Play:");
                    
                    return;
            }

            if (client.Socket != Admin && !Options.AllowNonAdminSkipping)
            {
                await Respond(client.Socket, "Invalid permission");
                return;
            }

            switch (command)
            {
                case "queue":
                    Queue = JsonSerializer.Deserialize<List<SearchResult>>(joined);
                    await Broadcast($"Queue:{joined}", client);
                    return;
                
                case "back":
                    if (Current - 1 < 0)
                        return;
                    ClearReady();
                    await Broadcast("Back:");
                    return;
                
                case "skip":
                    if (Current + 1 != Queue.Count)
                        return;
                    ClearReady();
                    await Broadcast("Skip:");
                    return;
                
                case "goto":
                    var parsed = int.TryParse(joined, out var num);
                    if (!parsed)
                    {
                        await Respond(client.Socket, "Invalid number syntax");
                        return;
                    }
                    if (Current + 1 == Queue.Count || Current - 1 < 0)
                        return;
                    ClearReady();
                    await Broadcast($"GoTo:{num}");
                    return;

                case "options":
                    Options = JsonSerializer.Deserialize<Settings>(joined);
                    return;
                
                case "ban":
                    return;
            }
        }

        private async Task Broadcast(string message)
        {
            List<Client> clients;

            lock (Clients)
            {
                clients = Clients.ToList();
            }

            foreach (var client in clients)
            {
                await Respond(client.Socket, message);
            }
        }
        
        private async Task Broadcast(string message, Client exclude)
        {
            List<Client> clients;

            lock (Clients)
            {
                clients = Clients.ToList();
            }

            if (exclude != null)
            {
                clients.Remove(exclude);
            }
            
            foreach (var client in clients)
            {
                await Respond(client.Socket, message);
            }
        }
        
        private static async Task Respond(WebSocket socket, string message)
        {
            try
            {
                if (!socket.IsConnected) return;
                await socket.WriteStringAsync(message);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Responding to audio socket failed: \"{e}\"");
            }
        }

        public async Task AddClient(WebSocket ws, string suppliedToken)
        {
            try
            {
                var client = new Client
                {
                    Socket = ws,
                    Token = suppliedToken
                };
                lock (Clients)
                {
                    Clients.Add(client);
                }
                while (ws.IsConnected)
                {
                    try
                    {
                        var message = await ws.ReadStringAsync(CancellationToken.None);
                        if (message == null)
                        {
                            lock (Clients)
                            {
                                Clients.Remove(client);
                            }
                            return;
                        }
                        await OnWrite(client, message);
                    }
                    catch (Exception e)
                    { 
                        await Debug.WriteAsync($"Exception reading AudioSocket message: \"{e}\"");
                    }
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Client Session failed: \"{e}\"");
            }
        }
        
        public async Task SetAdmin(WebSocket ws, string suppliedToken = null)
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
            public WebSocket Socket { get; init; }
            public string Token { get; init; }
            public bool IsAnon => string.IsNullOrEmpty(Token) || Token is "null" or "undefined";
            public bool Ready { get; set; }
            public bool Ended { get; set; }
        }

        public class Settings
        {
            public bool AllowNonAdminSkipping { get; set; }
            public bool AllowAnonymousJoining { get; set; }
        }
    }
}