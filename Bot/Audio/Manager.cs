using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Audio.Platforms;
using BatToshoRESTApp.Audio.Platforms.Discord;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using HttpClient = BatToshoRESTApp.Readers.HttpClient;

namespace BatToshoRESTApp.Audio
{
    public static class Manager
    {
        public static readonly List<Player> Main = new();

        public static Player GetPlayer(DiscordChannel channel, DiscordClient client, int fail = 0)
        {
            //UDRI MAISTORE EDNA PO DJULEVA RAKIQ
            var failedGetAttempts = fail;
            try
            {
                lock (Main)
                {
                    if (Main.Any(pl => pl.VoiceChannel.Id == channel.Id))
                    {
                        Debug.Write($"Returning channel: \"{channel.Name}\" in guild: \"{channel.Guild.Name}\"");
                        failedGetAttempts = 0;
                        return Main.First(pl => pl.VoiceChannel.Id == channel.Id);
                    }

                    var conn = client.GetVoiceNext().GetConnection(channel.Guild);
                    if (conn == null)
                    {
                        var pl = new Player
                        {
                            CurrentClient = client, VoiceChannel = channel
                        };
                        Main.Add(pl);
                        Debug.Write(
                            $"Adding new item to dictionary: \"{channel.Name}\", in guild: \"{channel.Guild.Name}\", with id: \"{channel.GuildId}\"");
                        failedGetAttempts = 0;
                        return pl;
                    }

                    var list = Bot.Clients.Where(cl => cl.CurrentUser.Id != client.CurrentUser.Id)
                        .Where(cl => cl.Guilds.ContainsKey(channel.Guild.Id));
                    foreach (var cl in from cl in list
                        let con = cl.GetVoiceNext().GetConnection(channel.Guild)
                        where con is null
                        select cl)
                    {
                        Debug.Write($"Client is: {cl.CurrentUser.Id}, {cl.CurrentUser.Username}");
                        var pl = new Player
                        {
                            CurrentClient = cl, VoiceChannel = cl.Guilds[channel.Guild.Id].Channels[channel.Id]
                        };
                        Main.Add(pl);
                        failedGetAttempts = 0;
                        return pl;
                    }
                }
            }
            catch (Exception e)
            {
                failedGetAttempts++;
                Debug.Write($"Get Player failed with: \"{e}\"", true, Debug.DebugColor.Urgent);
                return failedGetAttempts > 3 ? null : GetPlayer(channel, client, failedGetAttempts);
            }
            return null;
        }

        public static async Task PlayCommand(CommandContext ctx, string term, bool select = false)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the play command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await Bot.SendDirectMessage(ctx, "No free bot accounts in this guild. You can add more bot accounts from the bot site when it's made.");
                return;
            }

            try
            {
                await Play(term, select, player, userVoiceS, ctx.Member, ctx.Message.Attachments.ToList(), ctx.Channel);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Exception in Play: {e}");
                lock (Main)
                {
                    if (Main.Contains(player)) Main.Remove(player);
                }
                throw;
            }
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
                    var playerTask = new Task(async () => {
                        try
                        {
                            await player.Play();
                        }
                        catch (Exception e)
                        {
                            await Debug.WriteAsync($"Player Task Failed: {e}");
                        }});
                    playerTask.Start();
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
                            $"```Added: ({player.Queue.Items.IndexOf(items.First()) + 1}) - {items.First().GetName()}```");
                }
            }
            catch (Exception e)
            {
                try
                {
                    player.Disconnect();
                    player.Statusbar.Stop();
                    await Debug.WriteAsync($"Error in Play: {e}");
                }
                catch (Exception exception)
                {
                    await Debug.WriteAsync($"Failed to disconnect when caught error: {exception}");
                }
                throw;
            }
        }

        public static async Task Skip(CommandContext ctx, int times = 1)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the play command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;

            await player.Skip(times);
        }

        public static async Task Leave(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the leave command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            try
            {
                player.Statusbar.Stop();
                await player.DisconnectAsync();
                lock (Main)
                {
                    if (Main.Contains(player)) Main.Remove(player);
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Disconnecting exception: {e}");
            }
        }

        public static async Task Shuffle(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the shuffle command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            player?.Shuffle();
        }

        public static async Task Loop(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the loop command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            await ctx.RespondAsync("```Loop status is now: " + player.ToggleLoop() switch
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
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the pause command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            player?.Pause();
        }

        public static async Task PlayNext(CommandContext ctx, string term)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the play command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player.Connection == null)
            {
                await PlayCommand(ctx, term);
                return;
            }

            if (int.TryParse(term, out var nextSong))
            {
                var thing = player.Queue.Items[nextSong - 1];
                player.Queue.RemoveFromQueue(thing);
                player.Queue.AddToQueueNext(thing);
                await Bot.Reply(player.CurrentClient, ctx.Channel, $"Playing: ({player.Queue.Items.IndexOf(thing) + 1}) - \"{thing.GetName()}\" after this.");
                return;
            }

            List<IPlayableItem> item;
            if (ctx.Message.Attachments.Count > 0)
            {
                item = await new Search().Get(term, ctx.Message.Attachments.ToList(), ctx.Guild.Id);
                term = ctx.Message.Attachments.Count switch
                {
                    1 => ctx.Message.Attachments.ToList()[0].FileName, _ => "Discord Attachments"
                };
            }
            else
                item = await new Search().Get(term);
            item.ForEach(it => it.SetRequester(ctx.Member));
            player.Queue.AddToQueueNext(item);
            await Bot.Reply(player.CurrentClient, ctx.Channel, $"Playing: {(item.Count > 1 ? $"\"{term}\"" : $"({player.Queue.Items.IndexOf(item[0]) + 1}) - \"{item[0].GetName()}\"")} after this.");
        }

        public static async Task Remove(CommandContext ctx, string text)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the remove command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            var item = int.TryParse(text, out var num) ? await player.RemoveFromQueue(num - 1) : await player.RemoveFromQueue(text);
            if (item == null)
            {
                await Bot.Reply(player.CurrentClient, ctx.Channel, $"Failed to remove: \"{text}\"");
                return;
            }
            await Bot.Reply(player.CurrentClient, ctx.Channel, $"Removing {item.GetName()}");
        }

        public static async Task GetWebUi(CommandContext ctx)
        {
            if (BatTosho.WebUiUsers.ContainsKey(ctx.Member.Id))
            {
                var key = BatTosho.WebUiUsers[ctx.Member.Id];
                await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent($"```You have already generated a Web UI code: {key}```").WithEmbed(
                        new DiscordEmbedBuilder
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
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the move command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;

            var stuff = move.Split(" ");
            if (int.TryParse(stuff[0], out var thing1) && int.TryParse(stuff[1], out var thing2))
            {
                var succ = player.Queue.Move(thing1 - 1, thing2 - 1, out var item);
                if (succ) await Bot.Reply(player.CurrentClient, ctx.Channel, $"Moved ({thing1}) \"{item.GetName()}\" to ({thing2})");
                else await Bot.Reply(player.CurrentClient, ctx.Channel, "Failed to move.");
                return;
            }

            if (!move.Contains("!to"))
                await player.CurrentClient.SendMessageAsync(ctx.Channel, 
                    "```Invalid move format.\nYou must use two numbers or use the format specified below:\n\n" +
                    "-mv Exact Name !to Exact Name 2 ```");

            var tracks = move.Split("!to");
            var success = player.Queue.Move(tracks[0], tracks[1], out var i1, out var i2);
            if (success) await Bot.Reply(player.CurrentClient, ctx.Channel, $"Switched the places of \"{i1.GetName()}\" and \"{i2.GetName()}\"");
            else await Bot.Reply(player.CurrentClient, ctx.Channel, "Failed to move.");
        }

        public static async Task Shuffle(CommandContext ctx, int seedInt)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the shuffle command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            player?.Queue.ShuffleWithSeed(seedInt);
        }

        public static async Task GetSeed(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the shuffle command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            var seed = player.Queue.RandomSeed;
            await Bot.Reply(ctx,
                seed switch {0 => "This queue hasn't been shuffled.", _ => $"The queue's seed is: \"{seed}\""});
        }
        
        public static async Task List(CommandContext ctx, bool inDiscord)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the list command.");
                return;
            }
            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            switch (inDiscord)
            {
                case true:
                    await Bot.Reply(ctx,
                        new DiscordMessageBuilder().WithContent("```Current Queue:```").WithFile("queue.txt", new MemoryStream(Encoding.UTF8.GetBytes(player.Queue 
                            + "\n\nHere's a tech tip. " +
                            "\nYou can use the bot web interface which displays the list automatically. " +
                            "\nYou can add, remove and overall control the bot using a spicy looking interface. " +
                            "\nYou can use it with the -webui command. " +
                            "\nThe bot will DM you a link which you can use to login, and a token for authentication."))));
                    break;
                case false:
                    break;
            }    
        }
        
        public static async Task GoTo(CommandContext ctx, int index)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the GoTo command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            var thing = player?.GoToIndex(index + 1);
            await Bot.Reply(ctx, $"Going to ({index + 1}) - \"{thing?.GetName()}\"");
        }

        public static async Task Clear(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the shuffle command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            player.Queue.Clear();
        }

        public static async Task SavePlaylist(CommandContext ctx)
        {
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx,
                    "Enter a channel before using the continue to another server command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await Bot.SendDirectMessage(ctx, "The bot isn't in a channel.");
                return;
            }

            var token = $"{ctx.Guild.Id}-{ctx.Channel.Id}-{Bot.RandomString(6)}";
            var fs = SharePlaylist.Write(token, player.Queue.Items);
            fs.Position = 0;
            await ctx.RespondAsync(
                new DiscordMessageBuilder().WithContent($"```Queue saved sucessfully. \n\nYou can play it again with this command\"-p pl:{token}\", " +
                                                        "or by sending the attached file and using the play command```")
                    .WithFile($"{token}.batp",fs));
        }

        public static async Task SendLyrics(CommandContext ctx, string text)
        {
            string query;
            switch (string.IsNullOrEmpty(text))
            {
                case true:
                    var userVoiceS = ctx.Member.VoiceState.Channel;
                    if (userVoiceS == null)
                    {
                        await Bot.SendDirectMessage(ctx,
                            "Enter a channel before using the lyrics command without a search term.");
                        return;
                    }

                    var player = GetPlayer(userVoiceS, ctx.Client);
                    if (player == null)
                    {
                        await Bot.SendDirectMessage(ctx,
                            "The bot isn't in the channel. If you want to know the lyrics of a song add it's name after the command.");
                        return;
                    }

                    var item = player.Queue.GetCurrent();
                    query = $"{Regex.Replace(Regex.Replace(item.GetTitle(), @"\([^()]*\)", ""), @"\[[^]]*\]", "")}" +
                            $"{item.GetTitle().Contains('-') switch {true => "", false => $" - {Regex.Replace(Regex.Replace(item.GetAuthor(), "- Topic", ""), @"\([^()]*\)", "")}"}}";
                    break;

                case false:
                    query = text;
                    break;
            }

            var lyrics = await GetLyrics(query);
            if (lyrics == null)
            {
                await Bot.Reply(ctx, "No results found for this song");
                return;
            }
            if (lyrics.Length + query.Length + 13 + 6 > 2000)
            {
                await Bot.Reply(ctx,new DiscordMessageBuilder()
                    .WithContent("```The lyrics are longer than 2000 characters, which is Discord's length limit. Too bad. Sending song as a file.```")
                    .WithFile("lyrics.txt", new MemoryStream(Encoding.UTF8.GetBytes(lyrics)))
                );
                return;
            }

            await Bot.Reply(ctx, $"Lyrics for {query}: \n{lyrics}");
        }

        public static async Task<string> GetLyrics(string query)
        {
            var client = HttpClient.WithCookies();
            const string apiKey = "ce7175JINJTgC94aJFgeiwa7Bh99EaoqZFhTeFV9ejmpO2qjEXOpi1eR";
            var resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"https://api.happi.dev/v1/music?q={query}&limit=5&apikey={apiKey}&type=track&lyrics=1"));
            var response = await resp.Content.ReadAsStringAsync();
            var apiMusicResponse = JsonSerializer.Deserialize<LyricsApiStuff.HappiApiMusicResponse>(response);
            if (apiMusicResponse is null) throw new InvalidOperationException();
            //$"{apiMusicResponse.result.First().api_lyrics}?apikey={apiKey}"
            try
            {
                resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                    $"{apiMusicResponse.result.First().api_lyrics}?apikey={apiKey}"));
            }
            catch (Exception)
            {
                return "null";
            }
            response = await resp.Content.ReadAsStringAsync();
            var lyricsResponse = JsonSerializer.Deserialize<LyricsApiStuff.HappiApiLyricsResponse>(response);
            if (lyricsResponse is null) throw new InvalidOperationException();
            return lyricsResponse.result.lyrics;
        }
    }
}