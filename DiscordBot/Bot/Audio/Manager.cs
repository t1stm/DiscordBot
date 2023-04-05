#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms;
using DiscordBot.Data.Models;
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
using Result.Objects;
using HttpClient = DiscordBot.Readers.HttpClient;

namespace DiscordBot.Audio;

public static class Manager
{
    public static readonly List<Player> Main = new();

    public static Player? GetPlayer(DiscordChannel channel, DiscordClient client, int fail = 0,
        bool generateNew = false)
    {
        //UDRI MAISTORE EDNA PO DJULEVA RAKIQ
        var failedGetAttempts = fail;
        try
        {
            lock (Main)
            {
                if (Main.Any(pl => pl.VoiceChannel?.Id == channel.Id))
                {
                    Debug.Write($"Returning channel: \"{channel.Name}\" in guild: \"{channel.Guild.Name}\"");
                    failedGetAttempts = 0;
                    return Main.First(pl => pl.VoiceChannel?.Id == channel.Id);
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
                         where cl.GetVoiceNext().GetConnection(channel.Guild) is null
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
        var userVoiceS = ctx.Member!.VoiceState?.Channel;
        if (userVoiceS == null)
        {
            await Bot.SendDirectMessage(ctx, languageUser.EnterChannelBeforeCommand("play"));
            return;
        }

        var player = GetPlayer(userVoiceS, ctx.Client, generateNew: true);
        if (player == null)
        {
            await Bot.SendDirectMessage(ctx, languageUser.NoFreeBotAccounts());
            return;
        }

        player.Settings = await GuildSettings.FromId(ctx.Guild.Id);
        try
        {
            await Play(term, select, player, userVoiceS, ctx.Member, ctx.Message.Attachments.ToList(), ctx.Channel);
            if (!player.Started)
                lock (Main)
                {
                    if (Main.Contains(player)) Main.Remove(player);
                }
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"Exception in Play: {e}");
            throw;
        }
    }

    public static async Task Play(string? term, bool select, Player player, DiscordChannel userVoiceS,
        DiscordMember? user, List<DiscordAttachment>? attachments, DiscordChannel messageChannel)
    {
        try
        {
            term ??= string.Empty;
            var lang = player.Language;
            List<PlayableItem> items = new();
            var currentGuild = player.CurrentClient?.Guilds[userVoiceS.Guild.Id];
            player.Channel = currentGuild?.Channels[messageChannel.Id];
            if (select && !term.StartsWith("http"))
            {
                if (!await HandleSelect(term, player, user, messageChannel, lang, items))
                {
                    await Debug.WriteAsync("Bad select command");
                    return;
                }
            }
            else
            {
                var search = await Search.Get(term, attachments, user?.Guild.Id);
                if (search != Status.OK)
                    await messageChannel.SendMessageAsync(search.GetError().Stringify(lang).CodeBlocked());
                else
                    items = search.GetOK();
            }

            items.ForEach(it => it.SetRequester(user));
            player.Queue.AddToQueue(items);

            if (player.Started)
                switch (items.Count)
                {
                    case < 1:
                        return;
                    case > 1:
                        await player.CurrentClient!.SendMessageAsync(messageChannel,
                            lang.AddedItem(term).CodeBlocked());
                        return;
                    default:
                        await player.CurrentClient!.SendMessageAsync(messageChannel,
                            lang.AddedItem(
                                    $"({player.Queue.Items.IndexOf(items.First()) + 1}) - {items.First().GetName(player.Settings.ShowOriginalInfo)}")
                                .CodeBlocked());
                        return;
                }

            player.Started = true;
            var voiceNext = player.CurrentClient.GetVoiceNext();
            player.Connection = await voiceNext
                .ConnectAsync(currentGuild?.Channels[userVoiceS.Id]);
            player.VoiceChannel = userVoiceS;
            player.Sink = player.Connection?.GetTransmitSink();
            player.CurrentGuild = user?.Guild;

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
        catch (Exception e)
        {
            try
            {
                if (!player.Started)
                {
                    await player.DisconnectAsync();
                    player.Statusbar.Stop();
                }

                await Debug.WriteAsync($"Error in Play: {e}");
            }
            catch (Exception exception)
            {
                await Debug.WriteAsync($"Failed to disconnect when caught error: {exception}");
            }

            throw;
        }
    }

    private static async Task<bool> HandleSelect(string? term, Player player, DiscordUser? user,
        DiscordChannel messageChannel,
        ILanguage lang, ICollection<PlayableItem> items)
    {
        var results = await Search.Get(term, returnAllResults: true);
        List<PlayableItem> ok;
        if (results != Status.OK || (ok = results.GetOK()).Count < 1)
        {
            await messageChannel.SendMessageAsync(lang.NoResultsFound(term).CodeBlocked());
            return false;
        }

        var options = ok.Select(item =>
                new DiscordSelectComponentOption(item.GetName(), item.GetAddUrl(), item.Author))
            .ToList();
        var dropdown = new DiscordSelectComponent("dropdown", null, options);
        var builder = new DiscordMessageBuilder().WithContent(lang.SelectVideo())
            .AddComponents(dropdown);
        var message = await builder.SendAsync(player.Channel);
        var response = await message.WaitForSelectAsync(user, "dropdown", TimeSpan.FromSeconds(60));
        if (response.TimedOut)
        {
            await message.ModifyAsync(lang.SelectVideoTimeout().CodeBlocked());
            return false;
        }

        var interaction = response.Result.Values;
        if (interaction == null || interaction.Length < 1) return false;

        var search = await Search.Get(interaction.First());
        if (search != Status.OK)
        {
            await message.ModifyAsync(search.GetError().Stringify(lang).CodeBlocked());
        }
        else
        {
            var result = search.GetOK().FirstOrDefault();
            if (result == null)
            {
                await message.ModifyAsync("Unable to find result.".CodeBlocked());
                return false;
            }

            player.Statusbar.Message = await message.ModifyAsync(
                new DiscordMessageBuilder().WithContent(lang.ThisMessageWillUpdateShortly().CodeBlocked()));
            items.Add(result);
        }

        return true;
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        await ctx.RespondAsync(player.Language.LoopStatusUpdate(player.ToggleLoop()).CodeBlocked());
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
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
            do
            {
                if (nextSong > player.Queue.Count)
                {
                    await Bot.Reply(ctx, player.Language.NumberBiggerThanQueueLength(nextSong));
                    break;
                }

                var thing = player.Queue.Items[nextSong - 1];
                player.Queue.RemoveFromQueue(thing);
                player.Queue.AddToQueueNext(thing);
                await Bot.Reply(player.CurrentClient, ctx.Channel,
                    player.Language.PlayingItemAfterThis(player.Queue.Items.IndexOf(thing) + 1,
                        thing.GetName()));
                return;
            } while (
#pragma warning disable 162
                false); // This error is tilting me but I can't do anything about it, because it's technically true. Rider cannot contain my intelligence.
#pragma warning restore 162

        term += ""; // Clear any possible null warnings.

        var attachmentsList = ctx.Message.Attachments.ToList();

        var items = await Search.Get(term, attachmentsList, ctx.Guild.Id);

        term = attachmentsList.Count switch
        {
            1 => ctx.Message.Attachments.ToList()[0].FileName,
            _ => "Discord Attachments"
        };

        if (items == Status.Error)
        {
            await Bot.Reply(ctx, items.GetError().Stringify(player.Language));
            return;
        }

        var things = items.GetOK();

        things.ForEach(it => it.SetRequester(ctx.Member));
        player.Queue.AddToQueueNext(things);
        await Bot.Reply(player.CurrentClient, ctx.Channel,
            things.Count > 1
                ? player.Language.PlayingItemAfterThis(term)
                : player.Language.PlayingItemAfterThis(player.Queue.Items.IndexOf(things[0]) + 1,
                    things[0].GetName(player.Settings.ShowOriginalInfo)));
    }

    public static async Task Remove(CommandContext ctx, string? text)
    {
        text ??= "";
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        var item = int.TryParse(text, out var num)
            ? await player.RemoveFromQueue(num - 1)
            : await player.RemoveFromQueue(text);
        if (item == null)
        {
            await Bot.Reply(player.CurrentClient, ctx.Channel, player.Language.FailedToRemove(text));
            return;
        }

        await Bot.Reply(player.CurrentClient, ctx.Channel, player.Language.RemovingItem(item.GetName()));
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

    public static DiscordMessageBuilder GetWebUiMessage(string key, string text, string description)
    {
        return new DiscordMessageBuilder()
            .WithContent($"```{text}: {key}```")
            .AddFile("qr_code.jpg", GetQrCodeForWebUi(key))
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
            await ctx.Member.SendMessageAsync(GetWebUiMessage(user.Token,
                user.Language.YouHaveAlreadyGeneratedAWebUiCode(),
                user.Language.ControlTheBotUsingAFancyInterface()));
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).SendingADirectMessageContainingTheInformation());
            return;
        }

        var randomString = Bot.RandomString(96);
        await Controllers.Bot.AddUser(ctx.Member.Id, randomString);
        await ctx.Member.SendMessageAsync(GetWebUiMessage(randomString, user.Language.YourWebUiCodeIs(),
            user.Language.ControlTheBotUsingAFancyInterface()));
        await Bot.Reply(ctx, Parser.FromNumber(guild.Language).SendingADirectMessageContainingTheInformation());
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        var stuff = move.Split(" ");
        if (int.TryParse(stuff[0], out var thing1) && int.TryParse(stuff[1], out var thing2))
        {
            var succ = player.Queue.Move(thing1 - 1, thing2 - 1, out var item);
            if (succ)
                await Bot.Reply(player.CurrentClient, ctx.Channel,
                    player.Language.Moved(thing1, item.GetName(), thing2));
            else await Bot.Reply(player.CurrentClient, ctx.Channel, player.Language.FailedToMove());
            return;
        }

        if (!move.Contains("!to"))
            await player.CurrentClient!.SendMessageAsync(ctx.Channel, player.Language.InvalidMoveFormat());

        var tracks = move.Split("!to");
        var success = player.Queue.Move(tracks[0], tracks[1], out var i1, out var i2);
        if (success)
            await Bot.Reply(player.CurrentClient, ctx.Channel,
                player.Language.SwitchedThePlacesOf(i1.GetName(), i2.GetName()));
        else await Bot.Reply(player.CurrentClient, ctx.Channel, player.Language.FailedToMove());
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
            seed switch { 0 => "This queue hasn't been shuffled.", _ => $"The queue's seed is: \"{seed}\"" });
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        await Bot.Reply(ctx,
            new DiscordMessageBuilder().WithContent(player.Language.CurrentQueue().CodeBlocked()).AddFile(
                "queue.txt",
                new MemoryStream(Encoding.UTF8.GetBytes(player.Queue + player.Language.TechTip()))));
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        var thing = player.GoToIndex(index - 1);
        await Bot.Reply(ctx, player.Language.GoingTo(index, thing?.GetName()));
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        var val = player.UpdateVolume(volume);
        switch (val)
        {
            case true:
                await Bot.Reply(ctx, player.Language.SetVolumeTo(volume));
                break;
            case false:
                await Bot.Reply(ctx, player.Language.InvalidVolumeRange());
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        player.Queue.Clear();
    }

    public static string GetFreePlaylistToken(ulong? guildId, ulong? channelId)
    {
        guildId ??= 0;
        channelId ??= 0;
        var token = $"{guildId}/{channelId}/{Bot.RandomString(6)}";
        if (!Directory.Exists($"{Bot.WorkingDirectory}/Playlists/{guildId}"))
            Directory.CreateDirectory($"{Bot.WorkingDirectory}/Playlists/{guildId}");
        if (!Directory.Exists($"{Bot.WorkingDirectory}/Playlists/{guildId}/{channelId}/"))
            Directory.CreateDirectory($"{Bot.WorkingDirectory}/Playlists/{guildId}/{channelId}/");
        while (SharePlaylist.Exists(token)) token = $"{guildId}/{channelId}/{Bot.RandomString(6)}";
        return token;
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotIsNotInTheChannel());
            return;
        }

        var token = GetFreePlaylistToken(ctx.Guild.Id, player.VoiceChannel?.Id);
        FileStream fs;
        lock (player.Queue.Items)
        {
            fs = SharePlaylist.Write(token, player.Queue.Items);
            fs.Position = 0;
        }

        await ctx.RespondAsync(
            new DiscordMessageBuilder()
                .WithContent(player.Language.QueueSavedSuccessfully(token).CodeBlocked())
                .AddFile($"{token}.batp", fs));
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
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).OneCannotRecieveBlessingNothingToPlay());
            return;
        }

        player.PlsFix();
    }

    public static async Task SendLyrics(CommandContext ctx, string text)
    {
        string query;
        GuildsModel guild;
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
                    await Bot.Reply(ctx, Parser.FromNumber(guild.Language).BotNotInChannelLyrics());
                    return;
                }

                var item = player.Queue.GetCurrent();
                query =
                    $"{Regex.Replace(Regex.Replace(item?.GetTitle() ?? "", @"\([^()]*\)", ""), @"\[[^]]*\]", "")}" +
                    $"{(item?.GetTitle() ?? "").Contains('-') switch { true => "", false => $" - {Regex.Replace(Regex.Replace(item?.GetAuthor() ?? "", "- Topic", ""), @"\([^()]*\)", "")}" }}";
                break;

            case false:
                query = text;
                break;
        }

        guild = await GuildSettings.FromId(ctx.Guild.Id);

        var lyrics = await GetLyrics(query);
        if (lyrics == null)
        {
            await Bot.Reply(ctx, Parser.FromNumber(guild.Language).NoResultsFoundLyrics(query));
            return;
        }

        if (lyrics.Length + query.Length + 13 + 6 > 2000)
        {
            await Bot.Reply(ctx, new DiscordMessageBuilder()
                .WithContent(
                    Parser.FromNumber(guild.Language).LyricsLong().CodeBlocked())
                .AddFile("lyrics.txt", new MemoryStream(Encoding.UTF8.GetBytes(lyrics)))
            );
            return;
        }

        await Bot.Reply(ctx, $"Lyrics for {query}: \n{lyrics}");
    }

    public static async Task<string?> GetLyrics(string query)
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
                $"{apiMusicResponse.Result?.First().ApiLyrics}?apikey={apiKey}"));
        }
        catch (Exception)
        {
            return "null";
        }

        response = await resp.Content.ReadAsStringAsync();
        var lyricsResponse = JsonSerializer.Deserialize<LyricsApiStuff.HappiApiLyricsResponse>(response);
        if (lyricsResponse is null) throw new InvalidOperationException();
        return lyricsResponse.Result?.Lyrics;
    }
}