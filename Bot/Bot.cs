using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;

namespace BatToshoRESTApp
{
    public static class Bot
    {
        public enum RunType
        {
            Release = 0,
            Beta = 1
        }

        private const string BotRelease = "NzMxMjQ5MjMwNjEzNzA4OTMz.XwjS6g.4ciJLulvPl212VFvelwL9d9wBkw",
            BotBeta = "NjcxMDg3NjM4NjM1MDg1ODUy.Xi31EQ.v-QjHqPT6BAQhans6bveYhNC9CU",
            SecondaryBot = "OTAzMjg3NzM3Nzc4NTg5NzA2.YXqyQg.F3cDKz-icUbYYMUJXwLxT-BX574";

        public const string WorkingDirectory = "/home/kris/BatTosho";

        //private static Timer GarbageCollectTimer { get; } = new(60000);
        public const int UpdateDelay = 3200; //Milliseconds

        private static readonly Random Rng = new();
        private static Timer UpdateLoop { get; } = new(UpdateDelay);
        public static List<DiscordClient> Clients { get; } = new();
        public static bool DebugMode { get; private set; }

        private static List<DiscordMessage> BotMessages { get; } = new();

        //public static readonly HttpServer WebSocketServer = new (8000); //This is a server by the WebSocketSharp lib.

        public static async Task Initialize(RunType token)
        {
            //GarbageCollectTimer.Elapsed += (_, _) => { GC.Collect(); }; // I've removed this because it was a bad idea to call the GC manually. 
            //GarbageCollectTimer.Start();                                // Please if you're reading this don't do it.
            UpdateLoop.Elapsed += (_, _) => OnUpdate();
            UpdateLoop.Start();
            BatTosho.LoadUsers();
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
                    Clients.Add(MakeClient("OTA2MDc2NTM2Nzk5NjI1MjU3.YYTXiA.8GU3WJRqU_kWW7lhQ_upcH_mfGI", Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2ODA2Njg2MzMwOTQw.YYTXyA.uFzpZH2q3-XPIv5fXoqhMDEFD5g", Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc3MjAxMzg3MTEwNDEy.YYTYJg.DDYabJ6mCuI9pjidgkTFPAMVtWg", Array.Empty<string>()));
                    await Debug.WriteAsync($"Bat Tosho E Veche Velik! RunType = {token}, Token is: \"{BotRelease}\"");
                    break;
                case RunType.Beta:
                    Clients.Add(MakeClient(BotBeta, new[] {";"}, useSlashCommands: false));
                    Clients.Add(MakeClient(SecondaryBot, Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2NTM2Nzk5NjI1MjU3.YYTXiA.8GU3WJRqU_kWW7lhQ_upcH_mfGI", Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc2ODA2Njg2MzMwOTQw.YYTXyA.uFzpZH2q3-XPIv5fXoqhMDEFD5g", Array.Empty<string>()));
                    Clients.Add(MakeClient("OTA2MDc3MjAxMzg3MTEwNDEy.YYTYJg.DDYabJ6mCuI9pjidgkTFPAMVtWg", Array.Empty<string>()));
                    await Debug.WriteAsync($"Bat Tosho E Veche Velik! RunType = {token}, Token is: \"{BotBeta}\"");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }

            Clients.Add(SpecificContentBot()); //Ah yes, the specific content bot. Thank you very much.
            foreach (var client in Clients) await client.ConnectAsync();
            string text;
            var task = new Task(async () => await WebSocketServer.Start());
            task.Start();
            await ReadStatus(Clients[0]);
            while ((text = Console.ReadLine()) != "null")
                try
                {
                    switch (text)
                    {
                        case "list":
                        {
                            await Debug.WriteAsync("Listing all player instances:");
                            for (var i = 0; i < Manager.Main.Count; i++)
                            {
                                var pl = Manager.Main.ToList()[i];
                                await Debug.WriteAsync(
                                    $"\"{pl.CurrentGuild.Name}\" : \"{pl.VoiceChannel.Name} - {pl.VoiceChannel.Id}\"");
                            }

                            continue;
                        }
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
                                        $"{pl.CurrentGuild} : {pl.VoiceChannel?.Name} " +
                                        $"- Owner : {pl.CurrentGuild?.Owner?.DisplayName} - {pl.CurrentGuild?.Owner?.Id} " +
                                        $"- Track: ({pl.Queue?.Current + 1}) \"{pl.CurrentItem?.GetName()}\" - ({pl.Queue?.Count}) " +
                                        $"- Waiting Stopwatch: {Statusbar.Time(pl.WaitingStopwatch.Elapsed)} " +
                                        $"- Time: {Statusbar.Time(pl.Stopwatch.Elapsed)} " +
                                        $"- {Statusbar.Time(TimeSpan.FromMilliseconds(pl.CurrentItem.GetLength()))} " +
                                        $"- Paused: {pl.Paused} - Voice Users: {pl.VoiceUsers}");
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
                        case "clear":
                            Console.Clear();
                            await Debug.WriteAsync("Cleared the Console");
                            continue;
                        case "wusers":
                            await Debug.WriteAsync("Listing all Web Ui users:");
                            BatTosho.PrintUsers();
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
                var f = File.OpenWrite($"{WorkingDirectory}/Status.json");
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
                JsonWriteQueue.Update();
                Event.Update();
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
                MinimumLogLevel = LogLevel.None,
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
                        var cl = Manager.Main.FirstOrDefault(c => c.VoiceChannel.Id == args.Before.Channel.Id);
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
                    $"\"{args.Author.Username}#{args.Author.Discriminator}\": \"{args.Message.Content}\" in channel id \"{args.Channel.Id}\" - Message id is: {id}",
                    true, Debug.DebugColor.Warning);
            };
            if (!useSlashCommands) return client;
            var ext = client.UseSlashCommands();
            ext.RegisterCommands<CommandsSlash>();
            //ext.RegisterCommands<CommandsSlash>(933977766284652594);
            //// Same as the message on line 335, but I don't think I'll need to debug this anymore. Let's hope I didn't just jinx this just now.
            Debug.Write("Using Slash Commands.");

            return client;
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
            await ctx.RespondAsync(formatted ? $"```{text}```" : text);
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
            await client.SendMessageAsync(channel, formatted ? $"```{text}```" : text);
        }

        public static async Task SendDirectMessage(CommandContext ctx, string text, bool formatted = true)
        {
            await ctx.Member.SendMessageAsync(formatted ? $"```{text}```" : text);
        }

        public static string RandomString(int length, bool includeBadSymbols = false)
        {
            return new(Enumerable
                .Repeat(
                    $"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789{includeBadSymbols switch {true => "_-.", false => ""}}",
                    length).Select(s => s[new Random(Rng.Next(int.MaxValue)).Next(s.Length)]).ToArray());
        }

        public record BotActivity
        {
            public string Status { get; set; }
            public ActivityType ActivityType { get; set; }
        }
    }
}