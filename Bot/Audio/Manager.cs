using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;

namespace BatToshoRESTApp.Audio
{
    public static class Manager
    {
        public static readonly Dictionary<DiscordChannel, Player> Main = new();

        public static async Task<Player> GetPlayer(DiscordChannel channel, DiscordClient client)
        {
            //UDRI MAISTORE EDNA DJULEVA RAKIQ
            try
            {
                if (Main.ContainsKey(channel))
                {
                    await Debug.WriteAsync($"Returning channel: \"{channel.Name}\" in guild: \"{channel.Guild.Name}\"");
                    return Main[channel];
                }

                var conn = client.GetVoiceNext().GetConnection(channel.Guild);
                if (conn == null)
                {
                    Main.Add(channel, new Player
                    {
                        CurrentClient = client, VoiceChannel = channel
                    });
                    await Debug.WriteAsync(
                        $"Adding new item to dictionary: \"{channel.Name}\", in guild: \"{channel.Guild.Name}\", with id: \"{channel.GuildId}\"");
                    return Main[channel];
                }

                var list = Bot.Clients.Where(cl => cl.CurrentUser.Id != client.CurrentUser.Id)
                    .Where(cl => cl.Guilds.ContainsKey(channel.Guild.Id));
                foreach (var cl in from cl in list
                    let con = cl.GetVoiceNext().GetConnection(channel.Guild)
                    where con == null
                    select cl)
                {
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

        public static async Task PlayCommand(CommandContext ctx, string term, bool select = false)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.RespondAsync("```No free bot accounts in this guild.```");
                return;
            }

            await Play(term, select, player, userVoiceS, ctx.Member, ctx.Message.Attachments.ToList(), ctx.Channel);
        }

        public static async Task Play(string term, bool select, Player player, DiscordChannel userVoiceS,
            DiscordMember user, List<DiscordAttachment> attachments, DiscordChannel messageChannel)
        {
            try
            {
                if (player.Started == false)
                {
                    player.Started = true;
                    List<IPlayableItem> items;
                    if (attachments is {Count: > 0})
                    {
                        await Debug.WriteAsync($"Play message contains attachments: {attachments.Count}");
                        items = await new Search().Get(term, attachments, user.Guild.Id);
                        var builder =
                            new DiscordMessageBuilder().WithContent("```Hello! This message will update shortly.```");
                        player.Channel = player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[messageChannel.Id];
                        player.Statusbar.Message = await builder.SendAsync(player.Channel);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(term)) return;
                        player.Channel = player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[messageChannel.Id];
                        if (select && !term.Contains("http"))
                        {
                            var results = await new Video().SearchAllResults(term);
                            var options = results.Select(result =>
                                new DiscordSelectComponentOption(result.Title, result.GetId(), result.Author)).ToList();
                            var dropdown = new DiscordSelectComponent("dropdown", null, options);
                            var builder = new DiscordMessageBuilder().WithContent("Select a video.")
                                .AddComponents(dropdown);
                            var message = await builder.SendAsync(player.Channel);
                            var response = await message.WaitForSelectAsync(user, "dropdown", CancellationToken.None);
                            if (response.TimedOut) return;
                            var interaction = response.Result.Values;
                            items = new List<IPlayableItem>
                            {
                                await new Video().SearchById(interaction.First())
                            };
                            player.Statusbar.Message = await message.ModifyAsync(
                                new DiscordMessageBuilder().WithContent(
                                    "```Hello! This message will update shortly.```"));
                        }
                        else
                        {
                            items = await new Search().Get(term);
                        }
                    }

                    items.ForEach(it => it.SetRequester(user));
                    player.Queue.AddToQueue(items);
                    player.Connection = await player.CurrentClient.GetVoiceNext()
                        .ConnectAsync(player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[userVoiceS.Id]);
                    player.VoiceChannel = userVoiceS;
                    player.Sink = player.Connection.GetTransmitSink();
                    player.CurrentGuild = user.Guild;
                    await player.Play();
                    player.Disconnect();
                    await Debug.WriteAsync(
                        $"Disconnecting from channel: {player.VoiceChannel.Name} in guild: {player.CurrentGuild.Name}");
                    Main.Remove(userVoiceS);
                }
                else
                {
                    List<IPlayableItem> items;
                    if (attachments is {Count: > 0})
                    {
                        await Debug.WriteAsync($"Play message contains attachments: {attachments.Count}");
                        items = await new Search().Get(term, attachments, user.Guild.Id);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(term))
                        {
                            player.Paused = false;
                            return;
                        }
                        if (select && !term.Contains("http"))
                        {
                            var results = await new Video().SearchAllResults(term);
                            var options = results.Select(result =>
                                new DiscordSelectComponentOption(result.Title, result.GetId(), result.Author)).ToList();
                            var dropdown = new DiscordSelectComponent("dropdown", null, options);
                            var builder = new DiscordMessageBuilder().WithContent("Select a video.")
                                .AddComponents(dropdown);
                            var message = await builder.SendAsync(player.Channel);
                            var response = await message.WaitForSelectAsync(user, "dropdown", CancellationToken.None);
                            if (response.TimedOut) return;
                            var interaction = response.Result.Values;
                            items = new List<IPlayableItem>
                            {
                                await new Video().SearchById(interaction.First())
                            };
                            await message.DeleteAsync();
                        }
                        else
                        {
                            items = await new Search().Get(term);
                        }
                    }

                    items.ForEach(it => it.SetRequester(user));
                    player.Queue.AddToQueue(items);
                    if (items.Count > 1)
                        await player.CurrentClient.SendMessageAsync(messageChannel, $"```Added: {term}```");
                    else
                        await player.CurrentClient.SendMessageAsync(messageChannel,
                            $"```Added: {items.First().GetName()}```");
                }
            }
            catch (Exception e)
            {
                try
                {
                    player.Connection.Disconnect();
                    player.Statusbar.Stop();
                    await Debug.WriteAsync($"Error in Play: {e}");
                }
                catch (Exception exception)
                {
                    await Debug.WriteAsync($"Failed to disconnect when caught error: {exception}");
                }

                Main.Remove(player.VoiceChannel);
                throw;
            }
        }

        public static async Task Skip(CommandContext ctx, int times = 1)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;

            await player.Skip(times);
        }

        public static async Task Leave(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the leave command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            try
            {
                player.Disconnect();
                player.Statusbar.Stop();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Disconnecting exception: {e}");
                Main.Remove(player.VoiceChannel);
            }

            Main.Remove(player.VoiceChannel);
        }

        public static async Task Shuffle(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the shuffle command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            player?.Shuffle();
        }

        public static async Task Loop(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the loop command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            player.ToggleLoop();
            await ctx.RespondAsync("```Loop status is now: " + player.LoopStatus switch
            {
                Enums.Loop.None => "None", Enums.Loop.WholeQueue => "Looping whole queue.",
                Enums.Loop.One => "One Item Only.", _ => "None"
            } + "```");
        }

        public static async Task Pause(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the pause command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            player?.Pause();
        }

        public static async Task PlayNext(CommandContext ctx, string term)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the play command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player.Connection == null)
            {
                await PlayCommand(ctx, term);
                return;
            }

            if (int.TryParse(term, out int nextSong))
            {
                var thing = player.Queue.Items[nextSong - 1];
                player.Queue.RemoveFromQueue(thing);
                player.Queue.AddToQueueNext(thing);
                return;
            }
            List<IPlayableItem> item;
            if (ctx.Message.Attachments.Count > 0)
                item = await new Search().Get(term, ctx.Message.Attachments.ToList(), ctx.Guild.Id);
            else
                item = await new Search().Get(term);
            item.ForEach(it => it.SetRequester(ctx.Member));
            player.Queue.AddToQueueNext(item);
        }

        public static async Task Remove(CommandContext ctx, string text)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the remove command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;

            if (int.TryParse(text, out var num))
                player.Queue.RemoveFromQueue(num - 1);
            else player.Queue.RemoveFromQueue(text);
        }

        public static async Task GetWebUi(CommandContext ctx)
        {
            if (BatTosho.WebUiUsers.ContainsKey(ctx.Member.Id))
            {
                var key = BatTosho.WebUiUsers[ctx.Member.Id];
                await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent($"```You have already generated a Web UI code: {key}```").WithEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Bai Tosho Web Interface",
                        Url = "https://dankest.gq/BaiToshoBeta",
                        Description = "Control the bot using a fancy interface.",
                        Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                        {
                            Url = "https://dankest.gq/BaiToshoBeta/tosho.png"
                        }
                    }));
                return;
            }

            var randomString = Bot.RandomString(96);
            BatTosho.AddUser(ctx.Member.Id, randomString);
            await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent($"```Your Web UI Code is: {randomString}```").WithEmbed(new DiscordEmbedBuilder
                {
                    Title = "Bai Tosho Web Interface",
                    Url = "https://dankest.gq/BaiToshoBeta",
                    Description = "Control the bot using a fancy interface.",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = "https://dankest.gq/BaiToshoBeta/tosho.png"
                    }
                }));
        }

        public static async Task Move(CommandContext ctx, string move)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the move command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;

            var stuff = move.Split(" ");
            if (int.TryParse(stuff[0], out var thing1) && int.TryParse(stuff[1], out var thing2))
            {
                player.Queue.Move(thing1 - 1, thing2 - 1);
                return;
            }

            if (!move.Contains("!to"))
                await player.CurrentClient.SendMessageAsync(ctx.Channel, "```Invalid move format```");

            var tracks = move.Split("!to");
            player.Queue.Move(tracks[0], tracks[1]);
        }

        public static async Task Shuffle(CommandContext ctx, int seedInt)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the shuffle command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            player?.Queue.ShuffleWithSeed(seedInt);
        }

        public static async Task GetSeed(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the shuffle command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            var seed = player.Queue.RandomSeed;
            await Bot.Reply(ctx,
                seed switch {0 => "This queue hasn't been shuffled.", _ => $"The queue's seed is: \"{seed}\""});
        }

        public static async Task GoTo(CommandContext ctx, int index)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.Member.SendMessageAsync("```Enter a channel before using the shuffle command.```");
                return;
            }

            var player = await GetPlayer(userVoiceS, ctx.Client);
            player?.GoToIndex(index);
        }
    }
}