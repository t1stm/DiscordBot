using System;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Audio.Platforms.Discord;
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

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
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

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
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

            Manager.Main.Remove(player);
        }

        [SlashCommand("skip", "This is the skip command. It makes the bot skip an item.")]
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

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
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
                Content = $"```Skipping {(times == 1 ? $"{times} times" : "one time")}.```"
            });

            await player.Skip((int) times);
        }

        [SlashCommand("pause", "This is the pause command. It pauses the current item.")]
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

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
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

        [SlashCommand("shuffle", "This command shuffles the current queue")]
        public async Task Shuffle(InteractionContext ctx)
        {
            try
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

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder
                    {
                        Content = "```No free bot accounts.```"
                    });
                    return;
                }
                player.Shuffle();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Slash Command Shuffle failed. {e}");
            }            
        }
        [SlashCommand("getwebui", "This command gives you the code for the web interface of the bot.")]
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

        [SlashCommand("remove", "This command removes an item from the queue.")]
        public async Task Remove(InteractionContext ctx, [Option("num", "Index to remove")] long num)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder
                {
                    Content = "```Hello!```"
                });
            var userVoiceS = ctx.Member.VoiceState.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("```Enter a channel before using the remove command.```"));
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null) return;
            var item = player.Queue.RemoveFromQueue((int)num - 1);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Removing {item.GetName()}"));
        }

        [SlashCommand("saveplaylist", "This command saves the playlist and sends it to the current text channel")]
        public async Task SavePlaylist(InteractionContext ctx)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("```Hello!'''"));
                var userVoiceS = ctx.Member.VoiceState.Channel;
                if (userVoiceS == null)
                {
                    //await Bot.SendDirectMessage(ctx,
                    //    "Enter a channel before using the continue to another server command.");
                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder
                        {
                            Content = "```Enter a channel before using the continue to another server command```"
                        });
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder
                    {
                        Content = "```The bot isn't in a channel.```"
                    });
                    return;
                }

                var token = $"{ctx.Guild.Id}-{ctx.Channel.Id}-{Bot.RandomString(6)}";
                var fs = SharePlaylist.Write(token, player.Queue.Items);
                fs.Position = 0;
                await ctx.EditResponseAsync( 
                    new DiscordWebhookBuilder()
                        .WithContent($"```Queue saved sucessfully. \n\nYou can play it again with this command\"-p pl:{token}\", " +
                                                                        "or by sending the attached file and using the play command```").AddFile($"{token}.batp",fs));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Save Playlist Slash Command failed: {e}");
            }
        }
    }
}