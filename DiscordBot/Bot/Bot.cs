using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using DiscordBot.Audio;
using DiscordBot.Audio.Objects;
using DiscordBot.Data;
using DiscordBot.Messages;
using DiscordBot.Methods;
using DiscordBot.Objects;
using DiscordBot.Playlists;
using DiscordBot.Readers;
using DiscordBot.Readers.MariaDB;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using LoggerFactory = DiscordBot.Methods.LoggerFactory;

namespace DiscordBot
{
    public static class Bot
    {
        public enum RunType
        {
            Release = 0,
            Beta = 1
        }

        private const string 
            BotRelease = "NzMxMjQ5MjMwNjEzNzA4OTMz.XwjS6g.4ciJLulvPl212VFvelwL9d9wBkw",
            BotBeta = "NjcxMDg3NjM4NjM1MDg1ODUy.Xi31EQ.v-QjHqPT6BAQhans6bveYhNC9CU",
            SecondaryBot = "OTAzMjg3NzM3Nzc4NTg5NzA2.YXqyQg.F3cDKz-icUbYYMUJXwLxT-BX574";

        public const string WorkingDirectory = "/nvme0/DiscordBot";
        public const string MainDomain = "dankest.gq";
        public const string SiteDomain = $"https://{MainDomain}";
        public const string WebUiPage = "WebUi";
        public const string Name = "Slavi Trifonov";

        public const int UpdateDelay = 3200; //Milliseconds

        public static readonly Random Rng = new();
        private static Timer UpdateLoop { get; } = new(UpdateDelay);
        public static List<DiscordClient> Clients { get; } = new();
        public static bool DebugMode { get; private set; }
        private static List<DiscordMessage> BotMessages { get; } = new();

        public static async Task Initialize(RunType token)
        {
            LoadDatabases();
            PlaylistManager.LoadPlaylistInfos(); 
            UpdateLoop.Elapsed += (_, _) => OnUpdate();
            UpdateLoop.Start();
            await Controllers.Bot.LoadUsers(true);
            HttpClient.WithCookies();
            if (!File.Exists($"{WorkingDirectory}/Status.json"))
                await UpdateStatus(null, "Chalga", ActivityType.ListeningTo);
            switch (token)
            {
                case RunType.Release:
                    Clients.Add(MakeClient(BotRelease, new[] {"=", "-"}, useSlashCommands: true,
                        useDefaultHelpCommand: false));
                    Clients.Add(MakeClient(BotBeta, Array.Empty<string>()));
                    Clients.Add(MakeClient(SecondaryBot, Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2NTM2Nzk5NjI1MjU3.YYTXiA.8GU3WJRqU_kWW7lhQ_upcH_mfGI",
                        Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2ODA2Njg2MzMwOTQw.YYTXyA.uFzpZH2q3-XPIv5fXoqhMDEFD5g",
                        Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc3MjAxMzg3MTEwNDEy.YYTYJg.DDYabJ6mCuI9pjidgkTFPAMVtWg",
                        Array.Empty<string>()));
                    await Debug.WriteAsync($"{Name} E Veche Velik! RunType = {token}, Token is: \"{BotRelease}\"");
                    break;
                case RunType.Beta:
                    Clients.Add(MakeClient(BotBeta, new[] {";"}, useSlashCommands: false));
                    Clients.Add(MakeClient(SecondaryBot, Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2NTM2Nzk5NjI1MjU3.YYTXiA.8GU3WJRqU_kWW7lhQ_upcH_mfGI",
                        Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2ODA2Njg2MzMwOTQw.YYTXyA.uFzpZH2q3-XPIv5fXoqhMDEFD5g",
                        Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc3MjAxMzg3MTEwNDEy.YYTYJg.DDYabJ6mCuI9pjidgkTFPAMVtWg",
                        Array.Empty<string>()));
                    await Debug.WriteAsync($"{Name} E Veche Velik! RunType = {token}, Token is: \"{BotBeta}\"");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }

            Clients.Add(SpecificContentBot()); //Ah yes, the specific content bot. Thank you very much.
            foreach (var client in Clients) await client.ConnectAsync();
            string text;
            await ReadStatus(Clients[0]);
            YoutubeOverride.UpdateOverrides();
            var task = new Task(async () => { await WebSocketServer.Start(); });
            task.Start();
            while ((text = Console.ReadLine()) != "null")
                try
                {
                    switch (text)
                    {
                        case "save":
                            SaveDatabases();
                            await Debug.WriteAsync("Saving all databases.");
                            break;
                        case "list":
                        {
                            await Debug.WriteAsync("Listing all player instances:");
                            for (var i = 0; i < Manager.Main.Count; i++)
                            {
                                var pl = Manager.Main.ToList()[i];
                                await Debug.WriteAsync(
                                    $"\"{pl?.CurrentGuild?.Name}\" : \"{pl?.VoiceChannel?.Name} - {pl?.VoiceChannel?.Id}\"");
                            }

                            continue;
                        }
                        case "as":
                            WebSocketServer.PrintAudioSockets();
                            break;
                        case "sta":
                            Standalone.Audio.PrintAudio();
                            break;
                        case "clearsta":
                            await Debug.WriteAsync("Clearing all cached audios.");
                            Standalone.Audio.RemoveStale(true);
                            break;
                        case "reboot":
                            var cancel = false;
                            var force = false;
                            var writeTask = new Task(() =>
                            {
                                string cs;
                                while ((cs = Console.ReadLine()) is not ("cancel" or "force"))
                                {
                                }

                                switch (cs)
                                {
                                    case "cancel":
                                        cancel = true;
                                        Debug.Write("Cancelled the reboot.", false, Debug.DebugColor.Warning);
                                        break;
                                    case "force":
                                        force = true;
                                        Debug.Write("Forcing the reboot.", false, Debug.DebugColor.Warning);
                                        break;
                                }
                            });
                            writeTask.Start();
                            while (Manager.Main.Count > 0 && !cancel && !force)
                            {
                                Console.Clear();
                                await Debug.WriteAsync("Waiting to restart.");
                                await Debug.WriteAsync("Active guilds: ");
                                foreach (var pl in Manager.Main)
                                    await Debug.WriteAsync(
                                        $"{pl?.CurrentGuild} : {pl.VoiceChannel?.Name} " +
                                        $"- Owner : {pl?.CurrentGuild?.Owner?.DisplayName} - {pl?.CurrentGuild?.Owner?.Id} " +
                                        $"- Track: ({pl?.Queue?.Current + 1}) \"{pl?.CurrentItem?.GetName()}\" - ({pl?.Queue?.Count}) " +
                                        $"- Waiting Stopwatch: {Statusbar.Time(pl?.WaitingStopwatch?.Elapsed ?? TimeSpan.Zero)} " +
                                        $"- Time: {Statusbar.Time(pl?.Stopwatch?.Elapsed ?? TimeSpan.Zero)} " +
                                        $"- {Statusbar.Time(TimeSpan.FromMilliseconds(pl?.CurrentItem?.GetLength() ?? 0))} " +
                                        $"- Paused: {pl?.Paused} - Voice Users: {pl?.VoiceUsers?.Count}");
                                await Task.Delay(1000);
                            }

                            if (cancel && !force)
                            {
                                force = false;
                                cancel = false;
                                break;
                            }

                            Environment.Exit(0);
                            break;
                        case "forceoff":
                            lock (Manager.Main)
                            {
                                for (var index = 0; index < Manager.Main.Count; index++)
                                {
                                    var pl = Manager.Main[index];
                                    pl.Disconnect(
                                        "Disconnecting due to an update in the bot's code. Sorry for the inconvenience.");
                                    Debug.Write($"Disconnecting: {index}");
                                }
                            }

                            Environment.Exit(0);
                            break;
                        case "updateoverrides":
                            YoutubeOverride.UpdateOverrides();
                            break;
                        case "clear":
                            Console.Clear();
                            await Debug.WriteAsync("Cleared the Console");
                            continue;
                        case "wusers":
                            await Debug.WriteAsync("Listing all Web Ui users:");
                            Controllers.Bot.PrintUsers();
                            continue;
                        case "guilds":
                            await Debug.WriteAsync("Listing all guilds:");
                            foreach (var kvp in Clients[0].Guilds)
                            {
                                var val = kvp.Value;
                                await Debug.WriteAsync(
                                    $"{val.Name} : {val.Id} - Owner : {val.Owner.DisplayName} - {val.Owner.Id}");
                            }

                            continue;
                        case "debug":
                            DebugMode = !DebugMode;
                            await Debug.WriteAsync($"Debug mode is now: {DebugMode}");
                            continue;
                        case "sms":
                            await Debug.WriteAsync("Enter a message (example: \"messageId&&Insert Message Here\")",
                                false, Debug.DebugColor.Urgent);
                            var sms = Console.ReadLine();
                            var smsSplit = sms.Split("&&");
                            if (smsSplit.Length != 2)
                            {
                                var mess = BotMessages.Last();
                                await mess.RespondAsync(smsSplit[1]);
                                continue;
                            }

                            var message = BotMessages.ElementAtOrDefault(int.Parse(smsSplit[0]));
                            await message?.RespondAsync(smsSplit[1]);
                            continue;
                        case "us":
                            await Debug.WriteAsync("Enter a new status (example: \"listening to&&kuceci\")", false,
                                Debug.DebugColor.Urgent);
                            var s = Console.ReadLine();
                            var sp = s.Split("&&");
                            if (sp.Length != 2) continue;
                            await UpdateStatus(Clients[0], sp[1], sp[0].ToLower().Trim() switch
                            {
                                "playing" => ActivityType.Playing,
                                "listening to" => ActivityType.ListeningTo,
                                "watching" => ActivityType.Watching,
                                "streaming" => ActivityType.Streaming,
                                "competing" => ActivityType.Competing,
                                _ => ActivityType.Playing
                            });
                            continue;
                        case "rs":
                            await ReadStatus(Clients[0]);
                            continue;
                    }
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync(e + "");
                }
        }

        public static void LoadDatabases()
        {
            Databases.FuckYoutubeDatabase.ReadDatabase();
            Databases.VideoDatabase.ReadDatabase();
            Databases.UserDatabase.ReadDatabase();
            Databases.GuildDatabase.ReadDatabase();
        }

        public static void SaveDatabases()
        {
            Databases.FuckYoutubeDatabase.SaveToFile();
            Databases.VideoDatabase.SaveToFile();
            Databases.UserDatabase.SaveToFile();
            Databases.GuildDatabase.SaveToFile();
        }

        private static async Task ReadStatus(DiscordClient client)
        {
            try
            {
                var f = File.OpenText($"{WorkingDirectory}/Status.json");
                var json = JsonSerializer.Deserialize<BotActivity>(f.BaseStream);
                if (json != null)
                    await client.UpdateStatusAsync(new DiscordActivity(json.Status, json.ActivityType));
                f.Close();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Reading status failed: {e}");
            }
        }

        private static async Task UpdateStatus(DiscordClient client, string status, ActivityType activity)
        {
            try
            {
                if (client != null)
                    await client.UpdateStatusAsync(new DiscordActivity(status, activity));
                var f = File.Open($"{WorkingDirectory}/Status.json", FileMode.Create, FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                f.Flush();
                await JsonSerializer.SerializeAsync(f, new BotActivity
                {
                    ActivityType = activity,
                    Status = status
                });
                f.Close();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Writing status failed: {e}");
            }
        }

        private static void OnUpdate() // This is updated with the global UpdateDelay integer.
        {
            // And for the sake of not crashing the bot when the spaghetti code acts up, I've enclosed it with try catch blocks.
            try // Genius. Insert head blown guy GIF here.
            {
                Standalone.Audio.RemoveStale();
                WebSocketServer.RemoveStale();
            }
            catch (Exception e)
            {
                Debug.Write($"UpdateLoop error: \"{e}\"", true, Debug.DebugColor.Warning);
            }
        }

        private static DiscordClient MakeClient(string token, IEnumerable<string> prefixes,
            bool useDefaultHelpCommand = false,
            bool useInteractivity = true, bool useVoiceNext = true, bool useSlashCommands = false)
        {
            var client = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                LoggerFactory = new LoggerFactory(),
                LogTimestampFormat = Debug.DebugTimeDateFormat
            });
            var commandsExtension = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = prefixes,
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = useDefaultHelpCommand
            });
            if (prefixes != null)
                client.VoiceStateUpdated += async (_, args) =>
                {
                    try
                    {
                        if (Clients.All(c => c.CurrentUser.Id != args.User.Id)) return;
                        if (Manager.Main.Count < 1) return;
                        if (args.Before == null) return;
                        var cl = Manager.Main.FirstOrDefault(c => c.VoiceChannel?.Id == args.Before.Channel.Id);
                        if (cl == null) return;
                        if (args.After.Channel == null && !cl.UpdatedChannel)
                        {
                            await Debug.WriteAsync("After Channel is Null", false, Debug.DebugColor.Warning);
                            await cl.DisconnectAsync();
                            return;
                        }

                        if (args.Before.Channel.Id == args.After.Channel?.Id) return;
                        cl.UpdateChannel(args.After.Channel);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Voice State Updated failed: {e}", false, Debug.DebugColor.Urgent);
                    }
                };
            commandsExtension.RegisterCommands<Commands>();
            if (useInteractivity)
                client.UseInteractivity(new InteractivityConfiguration
                {
                    PollBehaviour = PollBehaviour.KeepEmojis,
                    Timeout = TimeSpan.FromSeconds(60)
                });
            if (useVoiceNext)
                client.UseVoiceNext(new VoiceNextConfiguration
                {
                    AudioFormat = new AudioFormat(48000, 2, VoiceApplication.LowLatency),
                    EnableIncoming = false
                });
            client.MessageCreated += async (sender, args) =>
            {
                if (!args.Channel.IsPrivate && args.Channel.Guild != null) return;
                var id = BotMessages.Count;
                BotMessages.Add(args.Message);
                await Debug.WriteAsync(
                    $"Bot account: \"{sender.CurrentUser.Username}\" with id: \"{sender.CurrentUser.Id}\" recieved private message from " +
                    $"\"{args.Author.Username}#{args.Author.Discriminator}\": \n\"{args.Message.Content}\" \nin channel id \"{args.Channel.Id}\" - Message id is: {id}",
                    true, Debug.DebugColor.Warning);
            };
            client.ComponentInteractionCreated += MessageInteractionHandler;
            if (!useSlashCommands) return client;
            var ext = client.UseSlashCommands();
            ext.RegisterCommands<CommandsSlash>();
            //ext.RegisterCommands<CommandsSlash>(933977766284652594);
            //Same as the message on line 335, but I don't think I'll need to debug this anymore. Let's hope I didn't just jinx this just now.
            Debug.Write("Using Slash Commands.");

            return client;
        }

        private static async Task MessageInteractionHandler(DiscordClient client,
            ComponentInteractionCreateEventArgs eventArgs)
        {
            try
            {
                eventArgs.Handled = true;
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                Player pl;
                lock (Manager.Main)
                {
                    pl = Manager.Main.AsParallel().FirstOrDefault(r =>
                        r.Channel?.Id == eventArgs.Channel.Id && r.VoiceUsers.Contains(eventArgs.User));
                }

                var user = await User.FromId(eventArgs.User.Id);
                if (pl == null && !eventArgs.Id.StartsWith("resume:"))
                {
                    await eventArgs.Interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(
                            user.Language.YouAreNotInTheChannel()));
                    if (DebugMode)
                        await Debug.WriteAsync(
                            $"The user: \"{eventArgs.User.Username}#{eventArgs.User.Discriminator}\" interacted to a player message without being in the channel.");
                    return;
                }

                var verbose = user.VerboseMessages;
                var split = eventArgs.Id.Split(':');
                var command = split.First();

                switch (command)
                {
                    case "shuffle":
                        if (verbose)
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(
                                    user.Language.ShufflingTheQueue().CodeBlocked()));
                        pl?.Shuffle();
                        break;
                    case "skip":
                        if (verbose)
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(
                                    user.Language.SkippingOneTime().CodeBlocked()));
                        if (pl != null)
                            await pl.Skip();
                        break;
                    case "pause":
                        if (verbose)
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(
                                    (!pl?.Paused switch
                                    {
                                        true => user.Language.PausingThePlayer(),
                                        false => user.Language.UnpausingThePlayer()
                                    }).CodeBlocked()));
                        pl?.Pause();
                        break;
                    case "back":
                        if (pl != null)
                            await pl.Skip(-1);
                        if (verbose)
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(
                                    user.Language.SkippingOneTimeBack().CodeBlocked()));
                        break;
                    case "leave":
                        if (pl != null)
                            await pl.DisconnectAsync();
                        break;
                    case "webui":
                        if (verbose)
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(
                                    user.Language.SendingADirectMessageContainingTheInformation().CodeBlocked()));
                        DiscordMessageBuilder message;
                        if (Controllers.Bot.WebUiUsers.ContainsKey(eventArgs.User.Id))
                        {
                            var key = Controllers.Bot.WebUiUsers.GetValue(eventArgs.User.Id);
                            message = Manager.GetWebUiMessage(key, user.Language.YouHaveAlreadyGeneratedAWebUiCode(),
                                user.Language.ControlTheBotUsingAFancyInterface());
                        }
                        else
                        {
                            var randomString = RandomString(96);
                            await Controllers.Bot.AddUser(eventArgs.User.Id, randomString);
                            message = Manager.GetWebUiMessage(randomString, user.Language.YourWebUiCodeIs(),
                                user.Language.ControlTheBotUsingAFancyInterface());
                        }

                        await eventArgs.Guild.Members[eventArgs.User.Id].SendMessageAsync(message);
                        break;
                    case "resume":
                        var userVoiceS = eventArgs.Guild.Members[eventArgs.User.Id]?.VoiceState?.Channel;
                        if (userVoiceS == null)
                        {
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(user.Language
                                    .EnterChannelBeforeCommand("Play Saved Queue").CodeBlocked()));
                            break;
                        }

                        var player = Manager.GetPlayer(userVoiceS, client, generateNew: true);
                        if (player == null)
                        {
                            await eventArgs.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder {IsEphemeral = true}.WithContent(user.Language
                                    .NoFreeBotAccounts().CodeBlocked()));
                            return;
                        }

                        player.Settings = await GuildSettings.FromId(eventArgs.Guild.Id);

                        var task = new Task(async () =>
                        {
                            try
                            {
                                await Manager.Play($"pl:{string.Join(':', split[1..])}", false, player, userVoiceS,
                                    eventArgs.Guild.Members[eventArgs.User.Id], new List<DiscordAttachment>(),
                                    eventArgs.Channel);
                            }
                            catch (Exception e)
                            {
                                await Debug.WriteAsync($"Player from interaction message failed: \"{e}\"");
                            }
                        });
                        task.Start();
                        break;
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Message interaction failed: \"{e}\"", false, Debug.DebugColor.Error);
            }
        }

        private static DiscordClient SpecificContentBot()
        {
            var client = new DiscordClient(new DiscordConfiguration
            {
                Token = "OTQwNjMwMzQzNTg3ODcyNzc4.YgKMRQ.EsRo8pmgYCd6Gju23uSgTiqzSqM",
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.None
            });
            var ext = client.UseSlashCommands();
            ext.RegisterCommands<CommandsSpecific>();
            //ext.RegisterCommands<CommandsSpecific>(933977766284652594); // This is used only when debugging.

            return client;
        }

        public static async Task Reply(CommandContext ctx, string text, bool formatted = true)
        {
            await ctx.RespondAsync(formatted ? text.CodeBlocked() : text);
        }

        public static async Task Reply(CommandContext ctx, DiscordMessageBuilder builder)
        {
            await ctx.RespondAsync(builder);
        }

        public static async Task Reply(DiscordClient client, DiscordChannel channel, DiscordMessageBuilder builder)
        {
            await client.SendMessageAsync(channel, builder);
        }

        public static async Task Reply(DiscordClient client, DiscordChannel channel, string text, bool formatted = true)
        {
            await client.SendMessageAsync(channel, formatted ? text.CodeBlocked() : text);
        }

        public static async Task SendDirectMessage(CommandContext ctx, string text, bool formatted = true)
        {
            if (ctx.Member is not null) await ctx.Member.SendMessageAsync(formatted ? text.CodeBlocked() : text);
        }

        public static string RandomString(int length, bool includeBadSymbols = false)
        {
            return new(Enumerable
                .Repeat(
                    $"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789{includeBadSymbols switch {true => "_-.", false => ""}}",
                    length).Select(s => s[new Random(Rng.Next(int.MaxValue)).Next(s.Length)]).ToArray());
        }

        private record BotActivity
        {
            public string Status { get; init; }
            public ActivityType ActivityType { get; init; }
        }
    }
}