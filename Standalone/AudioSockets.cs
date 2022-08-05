using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Methods;
using vtortola.WebSockets;

namespace DiscordBot.Standalone
{
    public class AudioSocket
    {
        public WebSocket Admin { get; set; }
        public List<Client> Clients { get; } = new();
        public Guid SessionId { get; init; }

        private void OnWrite(Client client, string message)
        {
            // TODO: Implement Behavior
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
                        OnWrite(client, message);
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
    }
}