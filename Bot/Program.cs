using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;

namespace Bat_Tosho
{
    internal static class Program
    {
        /* The Bot Prefixes are dynamically changed with an inline switch when the authentication token is changed. Look at line 85. */
        public const string
            MainDirectory = "/home/kris/BatToshoBeta/"; 
        /* I should really change it to the location of the executable but fuck it. Individual Builds for the win. */

        private const string BotRelease = "NzMxMjQ5MjMwNjEzNzA4OTMz.XwjS6g.4ciJLulvPl212VFvelwL9d9wBkw",
            BotBeta = "NjcxMDg3NjM4NjM1MDg1ODUy.Xi31EQ.v-QjHqPT6BAQhans6bveYhNC9CU";

        /*Discord Authentications Tokens.
         *  Main account (Bat Tosho) "NzMxMjQ5MjMwNjEzNzA4OTMz.XwjS6g.4ciJLulvPl212VFvelwL9d9wBkw"
         *  Beta testing account (Bat Kiril): "NjcxMDg3NjM4NjM1MDg1ODUy.Xi31EQ.v-QjHqPT6BAQhans6bveYhNC9CU"
         */

        private static string DiscordAuthToken { get; set; } = BotBeta;

        public static readonly char[] BotPrefixes =
        {
            '=' /* This is the default prefix */,
            ';' /* And this is the beta prefix. It's used to debug the bot while the main instance is still running. */
        };

        private static Dictionary<string, ActivityType> _customStatuses = new();
        public static Random Rng = new();
        public static string IdleStatus = "World of Tanks"; // Initial bot status as seen by the name of the string.
        public static ActivityType IdleActivity = ActivityType.Playing;
        public static DiscordActivity DiscordActivity = new(IdleStatus, IdleActivity);

        public static readonly DiscordClient Discord = new(new DiscordConfiguration
        {
            Token = DiscordAuthToken,
            TokenType = TokenType.Bot,
            MinimumLogLevel = LogLevel.Information
        });

        private static void InitCustomStatuses()
        {
            /* If for some reason someone is reading this source code and is at the same time at my machine feel free to add more statuses. I won't be mad if you tell me first. */
            _customStatuses = new Dictionary<string, ActivityType>
            {
                {"World of Tanks", ActivityType.Playing},
                {"10 Hours of Nothing", ActivityType.ListeningTo},
                {"Porn", ActivityType.Watching},
                {"The Sweet Silence Not Being In A Channel", ActivityType.ListeningTo},
                {"With Your Mom's Pussy", ActivityType.Playing},
                {"On dank.gq", ActivityType.Streaming},
                {"Best Bot Awards 2021", ActivityType.Competing},
                {"Memes on http://dank.gq/Memes", ActivityType.Watching},
                {"Dead by Daylight", ActivityType.Playing},
                {"League of Legends", ActivityType.Playing},
                {"Minecraft", ActivityType.Playing},
                {"Terraria", ActivityType.Playing},
                {"Cities: Skylines", ActivityType.Playing},
                {"Netflix With His Girl", ActivityType.Watching},
                {"DJ Damian", ActivityType.ListeningTo},
                {"Rado Shisharkata", ActivityType.ListeningTo},
                {"Kondio - Ataka Mix", ActivityType.ListeningTo}
            };
        }

        public static async Task MainAsync(bool release = false)
        {
            await Debug.Write($"Bat Tosho Veche E Velik. Bot Tokens are: Release: \"{BotRelease}\", Beta: \"{BotBeta}\", Using: \"{release switch{false => nameof(BotBeta), true => nameof(BotRelease)}}\"");
            InitCustomStatuses();
            DiscordAuthToken = release switch {true => BotRelease, false => BotBeta};
            var commands = Discord.UseCommandsNext(new CommandsNextConfiguration
            {
                // ReSharper disable HeuristicUnreachableCode
                StringPrefixes = new[]
                {
                    $"{DiscordAuthToken switch {BotBeta => BotPrefixes[1], BotRelease => BotPrefixes[0], _ => BotPrefixes[0]}}"
                },
                EnableDefaultHelp = true,
                EnableMentionPrefix = true,
                EnableDms = true
            });

            Discord.UseInteractivity(new InteractivityConfiguration
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(60)
            });
            Discord.UseVoiceNext(new VoiceNextConfiguration
            {
                PacketQueueSize = 120,
                AudioFormat = new AudioFormat(48000)
            });
            commands.RegisterCommands<Commands>();
            await Discord.ConnectAsync(DiscordActivity);
            await WaitStatus(new CancellationToken());

            await Task.Delay(-1); // The bot won't just open for one second and stop afterwards because of this line.
            // If this line is missing I will probably forget about it and remember after 30 minutes.
        }

        private static async Task WaitStatus(CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(30 * 60 * 1000, cancellationToken);
                    while (Commands.Playing)
                        await Task.Delay(5000, cancellationToken);
                    await UpdateStatus();
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            }, cancellationToken);
        }

        private static async Task UpdateStatus()
        {
            try
            {
                var chosen = Rng.Next(0, _customStatuses.Count);
                var (status, activity) /* <-- This is nice. */ = _customStatuses.ElementAt(chosen);
                DiscordActivity.Name = IdleStatus = status;
                DiscordActivity.ActivityType = IdleActivity = activity;

                await Debug.Write
                ("Updated Status to " +
                 $"{DiscordActivity.ActivityType.ToString().Replace("ListeningTo", "Listening To").Replace("Competing", "Competing In")}" +
                 $" {DiscordActivity.Name} {DiscordActivity.StreamUrl}");

                await Discord.UpdateStatusAsync(
                    DiscordActivity); // 02 Aug 2021 Why was this run synchronously I don't even know.;
            }
            catch (Exception e)
            {
                await Debug.Write($"Updating Discord Activity Failed: {e}");
            }
        }
    }
}