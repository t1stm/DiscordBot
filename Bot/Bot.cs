using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using MySqlX.XDevAPI;

namespace BatToshoRESTApp
{
    public static class Bot
    {
        public static int UpdateDelay { get; set; } = 1600; //Milliseconds
        public enum RunType
        {
            Release = 0,
            Beta = 1
        }

        private const string BotRelease = "NzMxMjQ5MjMwNjEzNzA4OTMz.XwjS6g.4ciJLulvPl212VFvelwL9d9wBkw",
            BotBeta = "NjcxMDg3NjM4NjM1MDg1ODUy.Xi31EQ.v-QjHqPT6BAQhans6bveYhNC9CU",
            SecondaryBot = "OTAzMjg3NzM3Nzc4NTg5NzA2.YXqyQg.F3cDKz-icUbYYMUJXwLxT-BX574";

        public static string WorkingDirectory = "/home/kris/BatTosho";
        public static List<DiscordClient> Clients { get; } = new();

        public static async Task Initialize(RunType token)
        {
            switch (token)
            {
                case RunType.Release:
                    Clients.Add(MakeClient(BotRelease, new[] {"=", "-"}));
                    await Debug.WriteAsync($"Bat Tosho E Veche Velik! RunType = {token}, Token is: \"{BotRelease}\"");
                    break;
                case RunType.Beta:
                    Clients.Add(MakeClient(BotBeta, new[] {";"}));
                    Clients.Add(MakeClient(SecondaryBot, null));
                    await Debug.WriteAsync($"Bat Tosho E Veche Velik! RunType = {token}, Token is: \"{BotBeta}\"");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token, null);
            }
            
            foreach (var client in Clients) await client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static DiscordClient MakeClient(string token, string[] prefixes, bool useDefaultHelpCommand = true,
            bool useInteractivity = true, bool useVoiceNext = true)
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

            return client;
        }
    }
}