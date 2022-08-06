using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
                AllowNonAdminSkipping = false
            };
        }
        public WebSocket Admin { get; set; }
        public List<Client> Clients { get; } = new();
        public Guid SessionId { get; init; }

        private Settings Options;

        private async Task OnWrite(Client client, string message)
        {
            // TODO: Implement Behavior
            var split = message.Split(':');
            if (split.Length < 2)
            {
                await Respond(client.Socket, "Invalid syntax");
                return;
            }
            
            var joined = string.Join(':', split[1..]);
            var command = split[0].ToLower();
            
            switch (command)
            {
                case "info":
                    return;
            }

            if (client.Socket != Admin && !Options.AllowNonAdminSkipping)
            {
                await Respond(client.Socket, "Invalid permission");
                return;
            }

            switch (command)
            {
                case "updatecurrent":
                    return;
                
                case "options":
                    Options = JsonSerializer.Deserialize<Settings>(joined);
                    return;
                
                case "ban":
                    return;
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

        public struct Client
        {
            public WebSocket Socket { get; init; }
            public string Token { get; init; }
            public bool IsAnon => string.IsNullOrEmpty(Token);
        }

        public struct Settings
        {
            public bool AllowNonAdminSkipping { get; set; }
        }
    }
}