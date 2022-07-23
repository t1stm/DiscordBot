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
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms;
using DiscordBot.Audio.Platforms.Discord;
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Methods;
using DiscordBot.Miscellaneous;
using DiscordBot.Objects;
using DiscordBot.Readers.MariaDB;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using QRCoder;
using HttpClient = DiscordBot.Readers.HttpClient;

namespace DiscordBot.Audio
{
    public static class Manager
    {
        public static readonly List<Player> Main = new();

        public static Player GetPlayer(DiscordChannel channel, DiscordClient client, int fail = 0,
            bool generateNew = false)
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
                    switch (conn)
                    {
                        case null when !generateNew:
                            return null;
                        case null:
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
                return failedGetAttempts > 3 ? null : GetPlayer(channel, client, failedGetAttempts, generateNew);
            }

            return null;
        }

        public static async Task PlayCommand(CommandContext ctx, string term, bool select = false)
        {
            var user = await User.FromId(ctx.User.Id);
            var languageUser = user.Language;
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, languageUser.EnterChannelBeforeCommand("play"));
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client, generateNew: true);
            if (player == null)
            {
                await Bot.SendDirectMessage(ctx,languageUser.NoFreeBotAccounts());
                return;
            }
            player.Settings = await GuildSettings.FromId(ctx.Guild.Id);
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
                var lang = player.Settings.Language;
                if (!player.Started)
                {
                    player.Started = true;
                    List<PlayableItem> items;
                    if (attachments is {Count: > 0})
                    {
                        await Debug.WriteAsync($"Play message contains attachments: {attachments.Count}");
                        items = await Search.Get(term, attachments, user.Guild.Id);
                        var builder =
                            new DiscordMessageBuilder().WithContent(lang.ThisMessageWillUpdateShortly().CodeBlocked());
                        player.Channel = player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[messageChannel.Id];
                        player.Statusbar.Message = await builder.SendAsync(player.Channel);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(term)) return;
                        player.Channel = player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[messageChannel.Id];
                        if (select && !term.StartsWith("http"))
                        {
                            var results = await Video.SearchAllResults(term);
                            if (results.Count < 1)
                            {
                                await messageChannel.SendMessageAsync(lang.NoResultsFound(term).CodeBlocked());
                                return;
                            }
                            var options = results.Select(result =>
                                new DiscordSelectComponentOption(result.Title, result.GetId(), result.Author)).ToList();
                            var dropdown = new DiscordSelectComponent("dropdown", null, options);
                            var builder = new DiscordMessageBuilder().WithContent(lang.SelectVideo())
                                .AddComponents(dropdown);
                            var message = await builder.SendAsync(player.Channel);
                            var response = await message.WaitForSelectAsync(user, "dropdown", TimeSpan.FromSeconds(60));
                            if (response.TimedOut)
                            {
                                await message.ModifyAsync(lang.SelectVideoTimeout().CodeBlocked());
                                return;
                            }

                            var interaction = response.Result.Values;
                            items = new List<PlayableItem>
                            {
                                await Video.SearchById(interaction.First())
                            };
                            player.Statusbar.Message = await message.ModifyAsync(
                                new DiscordMessageBuilder().WithContent(lang.ThisMessageWillUpdateShortly().CodeBlocked()));
                        }
                        else
                        {
                            items = await Search.Get(term);
                        }
                    }
                    
                    if (items.Count < 1)
                    {
                        await messageChannel.SendMessageAsync(lang.NoResultsFound(term).CodeBlocked());
                    }
                    else
                    {
                        items.ForEach(it => it.SetRequester(user));
                        player.Queue.AddToQueue(items);
                    }
                    
                    player.Connection = await player.CurrentClient.GetVoiceNext()
                        .ConnectAsync(player.CurrentClient.Guilds[userVoiceS.Guild.Id].Channels[userVoiceS.Id]);
                    player.VoiceChannel = userVoiceS;
                    player.Sink = player.Connection.GetTransmitSink();
                    player.CurrentGuild = user.Guild;
                    var playerTask = new Task(async () =>
                    {
                        try
                        {
                            await player.Play();
                        }
                        catch (Exception e)
                        {
                            await Debug.WriteAsync($"Player Task Failed: {e}");
                        }
                    });
                    playerTask.Start();
                }
                else
                {
                    List<PlayableItem> items;
                    if (attachments is {Count: > 0})
                    {
                        await Debug.WriteAsync($"Play message contains attachments: {attachments.Count}");
                        items = await Search.Get(term, attachments, user.Guild.Id);
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
                            var results = await Video.SearchAllResults(term);
                            if (results.Count < 1)
                            {
                                await messageChannel.SendMessageAsync(lang.NoResultsFound(term).CodeBlocked());
                                return;
                            }
                            var options = results.Select(result =>
                                new DiscordSelectComponentOption(result.Title, result.GetId(), result.Author)).ToList();
                            var dropdown = new DiscordSelectComponent("dropdown", null, options);
                            var builder = new DiscordMessageBuilder().WithContent(lang.SelectVideo())
                                .AddComponents(dropdown);
                            var message = await builder.SendAsync(player.Channel);
                            var response = await message.WaitForSelectAsync(user, "dropdown", CancellationToken.None);
                            if (response.TimedOut)
                            {
                                await message.ModifyAsync(lang.SelectVideoTimeout().CodeBlocked());
                                return;
                            }
                            var interaction = response.Result.Values;
                            items = new List<PlayableItem>
                            {
                                await Video.SearchById(interaction.First())
                            };
                            await message.DeleteAsync();
                        }
                        else
                        {
                            items = await Search.Get(term);
                        }
                    }
                    if (items.Count < 1)
                    {
                        await messageChannel.SendMessageAsync(lang.NoResultsFound(term).CodeBlocked());
                        return;
                    }
                    items.ForEach(it => it.SetRequester(user));
                    player.Queue.AddToQueue(items);
                    if (items.Count > 1)
                        await player.CurrentClient.SendMessageAsync(messageChannel, lang.AddedItem(term).CodeBlocked());
                    else
                        await player.CurrentClient.SendMessageAsync(messageChannel, 
                            lang.AddedItem($"({player.Queue.Items.IndexOf(items.First()) + 1}) - {items.First().GetName(player.Settings.ShowOriginalInfo)}").CodeBlocked());
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
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            var user = await User.FromId(ctx.User.Id);
            var lang = user.Language;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, lang.EnterChannelBeforeCommand("skip"));
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;

            await player.Skip(times);
        }

        public static async Task Leave(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("leave"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            try
            {
                player.Statusbar.Stop();
                player.Disconnect();
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
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("shuffle"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            player.Shuffle();
        }

        public static async Task SendHelpMessage(DiscordChannel channel, string command = "home")
        {
            if (string.IsNullOrEmpty(command)) command = "home";
            if (command.StartsWith("-") || command.StartsWith("=") || command.StartsWith("/")) command = command[1..];
            var get = HelpMessages.GetMessage(command);
            if (get == null)
            {
                await channel.SendMessageAsync($"```Couldn't find command: {command}```");
                return;
            }

            await channel.SendMessageAsync(get);
        }

        public static async Task Loop(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("loop"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            await ctx.RespondAsync(player.Settings.Language.LoopStatusUpdate(player.ToggleLoop()).CodeBlocked());
        }

        public static async Task Pause(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("pause"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }
            
            player.Pause();
        }

        public static async Task PlayNext(CommandContext ctx, string term)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("loop"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player?.Connection == null)
            {
                await PlayCommand(ctx, term);
                return;
            }

            if (int.TryParse(term, out var nextSong))
            {
                do
                {
                    if (nextSong > player.Queue.Count)
                    {
                        await Bot.Reply(ctx, player.Settings.Language.NumberBiggerThanQueueLength(nextSong));
                        break;
                    }
                    var thing = player.Queue.Items[nextSong - 1];
                    player.Queue.RemoveFromQueue(thing);
                    player.Queue.AddToQueueNext(thing);
                    await Bot.Reply(player.CurrentClient, ctx.Channel,
                        player.Settings.Language.PlayingItemAfterThis(player.Queue.Items.IndexOf(thing) + 1, thing.GetName()));
                    return;
                } while (false); // This error is tilting me but I can't do anything about it, because it's technically true. Rider cannot contain my intelligence.
            }

            term += ""; // Clear any possible null warnings.

            List<PlayableItem> item;
            if (ctx.Message.Attachments.Count > 0)
            {
                item = await Search.Get(term, ctx.Message.Attachments.ToList(), ctx.Guild.Id);
                term = ctx.Message.Attachments.Count switch
                {
                    1 => ctx.Message.Attachments.ToList()[0].FileName, _ => "Discord Attachments"
                };
            }
            else
            {
                item = await Search.Get(term);
            }

            if (item.Count < 1)
            {
                await Bot.Reply(ctx, player.Settings.Language.NoResultsFound(term));
                return;
            }

            item.ForEach(it => it.SetRequester(ctx.Member));
            player.Queue.AddToQueueNext(item);
            await Bot.Reply(player.CurrentClient, ctx.Channel,
                item.Count > 1 ? player.Settings.Language.PlayingItemAfterThis(term) : player.Settings.Language.PlayingItemAfterThis(player.Queue.Items.IndexOf(item[0]) + 1, item[0].GetName(player.Settings.ShowOriginalInfo)));
        }

        public static async Task Remove(CommandContext ctx, string text)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("remove"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            var item = int.TryParse(text, out var num)
                ? await player.RemoveFromQueue(num - 1)
                : await player.RemoveFromQueue(text);
            if (item == null)
            {
                await Bot.Reply(player.CurrentClient, ctx.Channel, player.Settings.Language.FailedToRemove(text));
                return;
            }

            await Bot.Reply(player.CurrentClient, ctx.Channel, player.Settings.Language.RemovingItem(item.GetName()));
        }

        public static MemoryStream GetQrCodeForWebUi(string key)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode($"{Bot.SiteDomain}/{Bot.WebUiPage}?clientSecret={key}",
                QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeAsPngByteArr = qrCode.GetGraphic(4);
            return new MemoryStream(qrCodeAsPngByteArr);
        }

        public static DiscordMessageBuilder GetWebUiMessage(string key, string text , string description)
        {
            return new DiscordMessageBuilder()
                .WithContent($"```{text}: {key}```")
                .WithFile("qr_code.jpg", GetQrCodeForWebUi(key))
                .WithEmbed(new DiscordEmbedBuilder
                {
                    Title = $"{Bot.Name} Web Interface",
                    Url = $"{Bot.SiteDomain}/{Bot.WebUiPage}?clientSecret={key}",
                    Description = description,
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = $"{Bot.SiteDomain}/{Bot.WebUiPage}/tosho.png"
                    }
                });
        }
        
        public static async Task GetWebUi(CommandContext ctx)
        {
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var user = await User.FromId(ctx.User.Id);
            if (ctx.Member is null) return;
            if (!string.IsNullOrEmpty(user.Token))
            {
                await ctx.Member.SendMessageAsync(GetWebUiMessage(user.Token, user.Language.YouHaveAlreadyGeneratedAWebUiCode(), 
                    user.Language.ControlTheBotUsingAFancyInterface()));
                await Bot.Reply(ctx, guild.Language.SendingADirectMessageContainingTheInformation());
                return;
            }

            var randomString = Bot.RandomString(96);
            await Controllers.Bot.AddUser(ctx.Member.Id, randomString);
            await ctx.Member.SendMessageAsync(GetWebUiMessage(randomString, user.Language.YourWebUiCodeIs(), user.Language.ControlTheBotUsingAFancyInterface()));
            await Bot.Reply(ctx, guild.Language.SendingADirectMessageContainingTheInformation());
        }

        public static async Task Move(CommandContext ctx, string move)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("move"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            var stuff = move.Split(" ");
            if (int.TryParse(stuff[0], out var thing1) && int.TryParse(stuff[1], out var thing2))
            {
                var succ = player.Queue.Move(thing1 - 1, thing2 - 1, out var item);
                if (succ)
                    await Bot.Reply(player.CurrentClient, ctx.Channel,
                        player.Settings.Language.Moved(thing1, item.GetName(), thing2));
                else await Bot.Reply(player.CurrentClient, ctx.Channel, player.Settings.Language.FailedToMove());
                return;
            }

            if (!move.Contains("!to"))
                await player.CurrentClient.SendMessageAsync(ctx.Channel, player.Settings.Language.InvalidMoveFormat());

            var tracks = move.Split("!to");
            var success = player.Queue.Move(tracks[0], tracks[1], out var i1, out var i2);
            if (success)
                await Bot.Reply(player.CurrentClient, ctx.Channel,
                    player.Settings.Language.SwitchedThePlacesOf(i1.GetName(), i2.GetName()));
            else await Bot.Reply(player.CurrentClient, ctx.Channel, player.Settings.Language.FailedToMove());
        }

        public static async Task Shuffle(CommandContext ctx, int seedInt)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the shuffle command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await Bot.Reply(ctx, "The bot isn't in the channel.");
                return;
            }

            player.Queue.ShuffleWithSeed(seedInt);
        }

        public static async Task GetSeed(CommandContext ctx)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, "Enter a channel before using the shuffle command.");
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await Bot.Reply(ctx, "The bot isn't in the channel.");
                return;
            }

            var seed = player.Queue.RandomSeed;
            await Bot.Reply(ctx,
                seed switch {0 => "This queue hasn't been shuffled.", _ => $"The queue's seed is: \"{seed}\""});
        }

        public static async Task Queue(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("move"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            await Bot.Reply(ctx,
                new DiscordMessageBuilder().WithContent(player.Settings.Language.CurrentQueue().CodeBlocked()).WithFile("queue.txt",
                    new MemoryStream(Encoding.UTF8.GetBytes(player.Queue + player.Settings.Language.TechTip()))));
        }

        public static async Task GoTo(CommandContext ctx, int index)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("goto"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            var thing = player.GoToIndex(index - 1);
            await Bot.Reply(ctx,player.Settings.Language.GoingTo(index, thing?.GetName()));
        }
        
        public static async Task Volume(CommandContext ctx, double volume)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("volume"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            var val = player.UpdateVolume(volume);
            switch (val)
            {
                case true:
                    await Bot.Reply(ctx, player.Settings.Language.SetVolumeTo(volume));
                    break;
                case false:
                    await Bot.Reply(ctx, player.Settings.Language.InvalidVolumeRange());
                    break;
            }
        }

        public static async Task Clear(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("clear"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            player.Queue.Clear();
        }

        public static async Task SavePlaylist(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.EnterChannelBeforeCommand("saveplaylist"));  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.BotIsNotInTheChannel());
                return;
            }

            var token = $"{ctx.Guild.Id}-{ctx.Channel.Id}-{Bot.RandomString(6)}";
            while (SharePlaylist.Exists(token)) token = $"{ctx.Guild.Id}-{ctx.Channel.Id}-{Bot.RandomString(6)}";
            var fs = SharePlaylist.Write(token, player.Queue.Items);
            fs.Position = 0;
            await ctx.RespondAsync(
                new DiscordMessageBuilder().WithContent(player.Settings.Language.QueueSavedSuccessfully(token).CodeBlocked())
                    .WithFile($"{token}.batp", fs));
        }

        public static async Task PlsFix(CommandContext ctx)
        {
            var user = await User.FromId(ctx.User.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await Bot.SendDirectMessage(ctx, user.Language.OneCannotRecieveBlessingNotInChannel());  
                return;
            }

            var player = GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                await Bot.Reply(ctx, guild.Language.OneCannotRecieveBlessingNothingToPlay());
                return;
            }

            player.PlsFix();
        }

        public static async Task SendLyrics(CommandContext ctx, string text)
        {
            string query;
            GuildSettings guild;
            switch (string.IsNullOrEmpty(text))
            {
                case true:
                    var user = await User.FromId(ctx.User.Id);
                    var userVoiceS = ctx.Member?.VoiceState?.Channel;
                    if (userVoiceS == null)
                    {
                        await Bot.SendDirectMessage(ctx, user.Language.UserNotInChannelLyrics());  
                        return;
                    }

                    var player = GetPlayer(userVoiceS, ctx.Client);
                    if (player == null)
                    {
                        guild = await GuildSettings.FromId(ctx.Guild.Id);
                        await Bot.Reply(ctx, guild.Language.BotNotInChannelLyrics());
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

            guild = await GuildSettings.FromId(ctx.Guild.Id);
            
            var lyrics = await GetLyrics(query);
            if (lyrics == null)
            {
                await Bot.Reply(ctx, guild.Language.NoResultsFoundLyrics(query));
                return;
            }

            if (lyrics.Length + query.Length + 13 + 6 > 2000)
            {
                await Bot.Reply(ctx, new DiscordMessageBuilder()
                    .WithContent(
                        guild.Language.LyricsLong().CodeBlocked())
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