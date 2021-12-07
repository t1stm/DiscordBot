using System;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Controllers;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace BatToshoRESTApp
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CommandsSlash : ApplicationCommandModule
    {
        [SlashCommand("play", "This is the play command. It plays music.")]
        public async Task PlayCommand(InteractionContext ctx, [Option("searchterm", "Search Term")] string term)
        {
            await ctx.CreateResponseAsync("Hello!");
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```You cannot use this command while not being in a channel.```"
                });
                return;
            }

            var player = await Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```No free bot accounts.```"
                });
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder
            {
                Content = $"```Running play command with search term: \"{term}\".```"
            });
            await Manager.Play(term, false, player, ctx.Member.VoiceState.Channel, ctx.Member, null, ctx.Channel);
        }

        [SlashCommand("leave", "This is the leave command. It makes the bot leave.")]
        public async Task LeaveCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Hello!");
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```You cannot use this command while not being in a channel.```"
                });
                return;
            }

            var player = await Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```No free bot accounts.```"
                });
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder
            {
                Content = "```Leaving.```"
            });

            player.Disconnect();
            player.Statusbar.Stop();

            Manager.Main.Remove(player.VoiceChannel);
        }

        [SlashCommand("skip", "This is the skip command. It makes the bot skip an item. Yes.")]
        public async Task SkipCommand(InteractionContext ctx, [Option("times", "Times to skip")] long times = 1)
        {
            await ctx.CreateResponseAsync("Hello!");
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```You cannot use this command while not being in a channel.```"
                });
                return;
            }

            var player = await Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```No free bot accounts.```"
                });
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder
            {
                Content = $"```Skipping {(times > 1 ? $"{times} times" : "one time")}.```"
            });

            await player.Skip((int) times);
        }

        [SlashCommand("pause", "This is the pause command. It pauses the current item. Yes.")]
        public async Task PauseCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Hello!");
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```You cannot use this command while not being in a channel.```"
                });
                return;
            }

            var player = await Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```No free bot accounts.```"
                });
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder
            {
                Content = "```Pausing the current item.```"
            });
            player.Pause();
        }

        [SlashCommand("getwebui", "This command messages you the code for the web interface of the bot.")]
        public async Task GetWebUi(InteractionContext ctx)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder
                    {
                        Content = "```Sending a Direct Message containing the required information.```"
                    });
                if (BatTosho.WebUiUsers.ContainsKey(ctx.Member.Id))
                {
                    var key = BatTosho.WebUiUsers[ctx.Member.Id];
                    await ctx.Member.SendMessageAsync($"```You have already generated a Web UI code: {key}```");
                    return;
                }

                var randomString = Bot.RandomString(96);
                BatTosho.AddUser(ctx.Member.Id, randomString);
                await ctx.Member.SendMessageAsync($"```Your Web UI Code is: {randomString}```");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Slash Command: {nameof(GetWebUi)} threw error: {e}");
            }
        }
    }
}