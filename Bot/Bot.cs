using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebSocketSharper.Server;

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

        private static Random Rng = new();

        public const string WorkingDirectory = "/home/kris/BatTosho";

        //private static Timer GarbageCollectTimer { get; } = new(60000);
        public static int UpdateDelay { get; set; } = 1600; //Milliseconds
        public static List<DiscordClient> Clients { get; } = new();

        public static readonly HttpServer WebSocketServer = new (NullLogger.Instance, 8000); //This is a server by the WebSocketSharp lib.

        public static async Task Initialize(RunType token)
        {
            //GarbageCollectTimer.Elapsed += (_, _) => { GC.Collect(); };
            //GarbageCollectTimer.Start();
            BatTosho.LoadUsers();
            HttpClient.WithCookies();
            switch (token)
            {
                case RunType.Release:
                    Clients.Add(MakeClient(BotRelease, new[] {"=", "-"}, useSlashCommands: true));
                    Clients.Add(MakeClient(BotBeta, null));
                    Clients.Add(MakeClient(SecondaryBot, null));
                    Clients.Add(MakeClient("OTA2MDc2NTM2Nzk5NjI1MjU3.YYTXiA.8GU3WJRqU_kWW7lhQ_upcH_mfGI", null));
                    Clients.Add(MakeClient("OTA2MDc2ODA2Njg2MzMwOTQw.YYTXyA.uFzpZH2q3-XPIv5fXoqhMDEFD5g", null));
                    Clients.Add(MakeClient("OTA2MDc3MjAxMzg3MTEwNDEy.YYTYJg.DDYabJ6mCuI9pjidgkTFPAMVtWg", null));
                    await Debug.WriteAsync($"Bat Tosho E Veche Velik! RunType = {token}, Token is: \"{BotRelease}\"");
                    break;
                case RunType.Beta:
                    Clients.Add(MakeClient(BotBeta, new[] {";"}, useSlashCommands: true));
                    Clients.Add(MakeClient(SecondaryBot, null));
                    Clients.Add(MakeClient("OTA2MDc2NTM2Nzk5NjI1MjU3.YYTXiA.8GU3WJRqU_kWW7lhQ_upcH_mfGI", null));
                    Clients.Add(MakeClient("OTA2MDc2ODA2Njg2MzMwOTQw.YYTXyA.uFzpZH2q3-XPIv5fXoqhMDEFD5g", null));
                    Clients.Add(MakeClient("OTA2MDc3MjAxMzg3MTEwNDEy.YYTYJg.DDYabJ6mCuI9pjidgkTFPAMVtWg", null));
                    await Debug.WriteAsync($"Bat Tosho E Veche Velik! RunType = {token}, Token is: \"{BotBeta}\"");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }

            foreach (var client in Clients) await client.ConnectAsync();
            string text;
            WebSocketServer.Start();
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
                            while (Manager.Main.Count > 0)
                            {
                                Console.Clear();
                                await Debug.WriteAsync("Waiting to restart.");
                                await Debug.WriteAsync("Active guilds: ");
                                foreach (var val in Manager.Main)
                                    await Debug.WriteAsync(
                                        $"{val.CurrentGuild} : {val.VoiceChannel.Name} " +
                                        $"- Owner : {val.CurrentGuild.Owner.DisplayName} - {val.CurrentGuild.Owner.Id} " +
                                        $"- Track: {val.Queue.Current + 1} - {val.Queue.Count} " +
                                        $"- Waiting Stopwatch: {val.WaitingStopwatch.Elapsed:c}");
                                await Task.Delay(1000);
                            }

                            Environment.Exit(0);
                            break;
                        case "forceoff":
                            foreach (var pl in Manager.Main)
                            {
                                await pl.DisconnectAsync("Disconnecting due to an update in the bot's code. Sorry for the inconvenience.");
                            }
                            Environment.Exit(0);
                            break;
                        case "clear":
                            Console.Clear();
                            await Debug.WriteAsync("Cleared the Console");
                            break;
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
                    }
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync(e + "");
                }
        }

        private static DiscordClient MakeClient(string token, IEnumerable<string> prefixes,
            bool useDefaultHelpCommand = true,
            bool useInteractivity = true, bool useVoiceNext = true, bool useSlashCommands = false)
        {
            var client = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.None
            });
            var commandsExtension = client.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = prefixes,
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = useDefaultHelpCommand
            });
            if (prefixes != null)
            {
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
                            await Debug.WriteAsync("After Channel is Null");
                            await cl.DisconnectAsync(isEvent:true);
                            Manager.Main.Remove(cl);
                            return;
                        }
                        if (args.Before.Channel.Id == args.After.Channel.Id) return;
                        cl.UpdateChannel(args.After.Channel);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Voice State Updated failed: {e}");
                    }
                };
            }
            commandsExtension.RegisterCommands<Commands>();
            if (useInteractivity)
                client.UseInteractivity(new InteractivityConfiguration
                {
                    PollBehaviour = PollBehaviour.KeepEmojis,
                    Timeout = TimeSpan.FromSeconds(60)
                });
            if (useVoiceNext) client.UseVoiceNext();
            if (!useSlashCommands) return client;
            var ext = client.UseSlashCommands();
            ext.RegisterCommands<CommandsSlash>();
            ext.RegisterCommands<CommandsSlash>(933977766284652594);
            Debug.Write("Using Slash Commands.");

            return client;
        }

        public static async Task Reply(CommandContext ctx, string text, bool formatted = true)
        {
            await ctx.RespondAsync(formatted ? $"```{text}```" : text);
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
    }
}