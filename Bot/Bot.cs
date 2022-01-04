using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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

        public static Random Rng = new();

        public static string WorkingDirectory = "/home/kris/BatTosho";
        private static Timer GarbageCollectTimer { get; } = new(60000);
        public static int UpdateDelay { get; set; } = 1600; //Milliseconds
        public static List<DiscordClient> Clients { get; } = new();

        public static async Task Initialize(RunType token)
        {
            GarbageCollectTimer.Elapsed += (_, _) => { GC.Collect(); };
            GarbageCollectTimer.Start();
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
                                var ke = Manager.Main.Keys.ToList()[i];
                                var val = Manager.Main.Values.ToList()[i];
                                await Debug.WriteAsync(
                                    $"\"{ke.Name} - {ke.Id}\":\"{val.Channel.Name} - {val.CurrentGuild.Name}\"");
                            }

                            continue;
                        }
                        case "reboot":
                            while (Manager.Main.Count > 0)
                            {
                                Console.Clear();
                                await Debug.WriteAsync("Waiting to restart.");
                                await Debug.WriteAsync("Active guilds: ");
                                foreach (var val in Manager.Main.Select(kvp => kvp.Value))
                                    await Debug.WriteAsync(
                                        $"{val.CurrentGuild} : {val.VoiceChannel.Name} " +
                                        $"- Owner : {val.CurrentGuild.Owner.DisplayName} - {val.CurrentGuild.Owner.Id} " +
                                        $"- Track: {val.Queue.Current + 1} - {val.Queue.Count}");
                                await Task.Delay(1000);
                            }

                            Environment.Exit(0);
                            break;
                        case "forceoff":
                            Environment.Exit(0);
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
            ext.RegisterCommands<CommandsSlash>(726854998503456779);
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