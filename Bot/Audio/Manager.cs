using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;

namespace BatToshoRESTApp.Audio
{
    public static class Manager
    {
        public static readonly Dictionary<DiscordChannel, Player> Main = new();

        public static async Task<Player> MethodNameHere(DiscordChannel channel, DiscordClient client)
        {
            //UDRI MAISTORE EDNA DJULEVA RAKIQ
            try
            {
                if (Main.ContainsKey(channel))
                {
                    await Debug.WriteAsync($"Returning channel that is contained in dictionary: {channel.Name}");
                    return Main[channel];
                };
                var conn = client.GetVoiceNext().GetConnection(channel.Guild);
                if (conn == null)
                {
                    Main.Add(channel, new Player
                    {
                        CurrentClient = client, VoiceChannel = channel
                    });
                    await Debug.WriteAsync("Adding new where conn is null");
                    return Main[channel];
                }
                var list = Bot.Clients.Where(cl => cl.CurrentUser.Id != client.CurrentUser.Id).Where(cl => cl.Guilds.ContainsKey(channel.Guild.Id));
                foreach (var cl in list)
                {
                    await Debug.WriteAsync($"Client: {cl.CurrentUser.Id}");
                    var con = cl.GetVoiceNext().GetConnection(channel.Guild);
                    if (con != null && Main.Values.All(val => val.CurrentClient.CurrentUser.Id != cl.CurrentUser.Id)) continue;
                    await Debug.WriteAsync($"Client is: {cl.CurrentUser.Id}, {nameof(cl)}");
                    Main.Add(channel, new Player
                    {
                        CurrentClient = cl, VoiceChannel = cl.Guilds[channel.Guild.Id].Channels[channel.Id]
                    });
                    return Main[channel];
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"{e}");
                return null;
            }
            return null;
        }
        
        public static async Task PlayCommand(CommandContext ctx, string term)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await MethodNameHere(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.RespondAsync("```No free bot accounts in this guild.```");
                return;
            }
            if (player.Started == false)
            {
                player.Started = true;
                player.Queue.AddToQueue(ctx.Message.Attachments.Count switch{>0 => await new Search().Get(term, ctx.Message.Attachments.ToList()), _ => await new Search().Get(term)});
                player.Connection = await player.CurrentClient.GetVoiceNext().ConnectAsync(player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[userVoiceS.Id]);
                player.VoiceChannel = userVoiceS;
                player.Sink = player.Connection.GetTransmitSink();
                player.CurrentGuild = ctx.Guild;
                player.Channel = player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[ctx.Channel.Id];
                await player.Play();
            }
            else
            {
                player.Queue.AddToQueue(ctx.Message.Attachments.Count switch{>0 => await new Search().Get(term, ctx.Message.Attachments.ToList()), _ => await new Search().Get(term)});
                return;
            }
            player.Disconnect();
            Main.Remove(player.VoiceChannel);
        }

        public static async Task Skip(CommandContext ctx, int times = 1)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await MethodNameHere(userVoiceS, ctx.Client);
            if (player == null)
            {
                return;
            }

            await player.Skip();
        }

        public static async Task Leave(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await MethodNameHere(userVoiceS, ctx.Client);
            if (player == null)
            {
                return;
            }
            player.Disconnect();
            await player.Statusbar.UpdateMessageAndStop("Bye!");
            Main.Remove(player.VoiceChannel);
            
        }

        public static async Task Shuffle(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await MethodNameHere(userVoiceS, ctx.Client);
            if (player == null)
            {
                return;
            }

            await player.Skip();
        }

        public static async Task Loop(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await MethodNameHere(userVoiceS, ctx.Client);
            if (player == null)
            {
                return;
            }
            var loop = player.ToggleLoop();
            await ctx.RespondAsync($"```Loop status is now: {loop.ToString()}```");
        }
    }
}