using System;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Audio;
using DiscordBot.Audio.Platforms.Discord;
using DiscordBot.Enums;
using DiscordBot.Methods;
using DiscordBot.Miscellaneous;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DiscordBot
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CommandsSlash : ApplicationCommandModule
    {
        public enum HelpCommandCategories
        {
            [ChoiceName("Home")] Home,
            [ChoiceName("Play")] Play,
            [ChoiceName("Play Next")] PlayNext,
            [ChoiceName("Play Select")] PlaySelect,
            [ChoiceName("Skip")] Skip,
            [ChoiceName("Leave")] Leave,
            [ChoiceName("Back")] Back,
            [ChoiceName("Shuffle")] Shuffle,
            [ChoiceName("Loop")] Loop,
            [ChoiceName("Pause")] Pause,
            [ChoiceName("Remove")] Remove,
            [ChoiceName("Move")] Move,
            [ChoiceName("List")] List,
            [ChoiceName("Clear")] Clear,
            [ChoiceName("WebUi")] WebUi,
            [ChoiceName("GoTo")] GoTo,
            [ChoiceName("Save Playlist")] SavePlaylist,
            [ChoiceName("Lyrics")] Lyrics,
            [ChoiceName("Get Avatar")] GetAvatar,
            [ChoiceName("Meme")] Meme,
            [ChoiceName("PlsFix")] PlsFix
        }

        public enum LoopStatus
        {
            [ChoiceName("Disable looping")] None,
            [ChoiceName("Loop whole queue")] LoopQueue,
            [ChoiceName("Loop one item")] LoopOne
        }

        [SlashCommand("play", "This is the play command. It plays music.")]
        public async Task PlayCommand(InteractionContext ctx, [Option("searchterm", "Search Term")] string term)
        {
            await ctx.CreateResponseAsync("Hello!");
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = "```You cannot use this command while not being in a channel.```"
                });
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client, generateNew: true);
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
            await Manager.Play(term, false, player, ctx.Member?.VoiceState?.Channel, ctx.Member, null, ctx.Channel);
        }

        [SlashCommand("leave", "This is the leave command. It makes the bot leave.")]
        public async Task LeaveCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Hello!");
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
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
                    Content = "```The bot isn't in the channel.```"
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

        [SlashCommand("help",
            "This command lists all the commands, a brief explaination of what they do and how to use them.")]
        public async Task SendHelpMessage(InteractionContext ctx, [Option("command", "Command to recieve help")]
            HelpCommandCategories cat = HelpCommandCategories.Home)
        {
            try
            {
                var command = cat switch
                {
                    HelpCommandCategories.Home => "home",
                    HelpCommandCategories.Play => "play",
                    HelpCommandCategories.PlayNext => "playnext",
                    HelpCommandCategories.PlaySelect => "playselect",
                    HelpCommandCategories.Skip => "skip",
                    HelpCommandCategories.Leave => "leave",
                    HelpCommandCategories.Back => "back",
                    HelpCommandCategories.Shuffle => "shuffle",
                    HelpCommandCategories.Loop => "loop",
                    HelpCommandCategories.Pause => "pause",
                    HelpCommandCategories.Remove => "remove",
                    HelpCommandCategories.Move => "move",
                    HelpCommandCategories.List => "list",
                    HelpCommandCategories.Clear => "clear",
                    HelpCommandCategories.WebUi => "webui",
                    HelpCommandCategories.GoTo => "goto",
                    HelpCommandCategories.SavePlaylist => "saveplaylist",
                    HelpCommandCategories.Lyrics => "lyrics",
                    HelpCommandCategories.GetAvatar => "getavatar",
                    HelpCommandCategories.Meme => "meme",
                    HelpCommandCategories.PlsFix => "plsfix",
                    _ => "home"
                };
                if (string.IsNullOrEmpty(command)) command = "home";
                if (command.StartsWith("-") || command.StartsWith("=") || command.StartsWith("/"))
                    command = command[1..];
                var get = HelpMessages.GetMessage(command);
                if (get == null)
                {
                    await ctx.CreateResponseAsync($"```Couldn't find command: {command}```");
                    return;
                }

                await ctx.CreateResponseAsync(get);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Sending help message in slash command failed. \"{e}\"");
            }
        }

        [SlashCommand("loop", "Choose loop type.")]
        public async Task LoopCommand(InteractionContext ctx, [Option("looptype", "The type of looping you want")]
            LoopStatus status)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                return;
            }

            player.LoopStatus = status switch
            {
                LoopStatus.None => Loop.None, LoopStatus.LoopOne => Loop.One,
                LoopStatus.LoopQueue => Loop.WholeQueue,
                _ => Loop.None
            };
            await ctx.CreateResponseAsync(
                $"```Loop status is now: {player.LoopStatus switch {Loop.None => "Disabling loop", Loop.One => "Looping one item", Loop.WholeQueue => "Looping whole queue", _ => "Some other option that doesn't have a type"}}.```");
        }

        public async Task MoveCommand(InteractionContext ctx, [Option("item", "The item which you want to move")]
            long x, [Option("place", "The place you want to place the item")]
            long y)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                return;
            }

            player.Queue.Move((int) x - 1, (int) y - 1, out var item);
            await ctx.CreateResponseAsync($"Moved ({x}) - \"{item.GetName()}\" to ({y})");
        }

        [SlashCommand("skip", "This is the skip command. It makes the bot skip an item.")]
        public async Task SkipCommand(InteractionContext ctx, [Option("times", "Times to skip")] long times = 1)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                return;
            }

            await ctx.CreateResponseAsync($"```Skipping {(times == 1 ? "one time" : $"{times} times")}.```"
            );

            await player.Skip((int) times);
        }

        [SlashCommand("pause", "This is the pause command. It pauses the current item.")]
        public async Task PauseCommand(InteractionContext ctx)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                return;
            }

            await ctx.CreateResponseAsync("```Pausing the current item.```");
            player.Pause();
        }

        [SlashCommand("shuffle", "This command shuffles the current queue")]
        public async Task Shuffle(InteractionContext ctx)
        {
            try
            {
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                    return;
                }

                player.Shuffle();
                await ctx.CreateResponseAsync("```Shuffling the queue.```");
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
                if (Controllers.Bot.WebUiUsers.ContainsKey(ctx.Member.Id))
                {
                    var key = Controllers.Bot.WebUiUsers[ctx.Member.Id];
                    //await ctx.Member.SendMessageAsync($"```You have already generated a Web UI code: {key}```");
                    await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"```Your Web UI Code is: {key}```")
                        .WithFile("qr_code.jpg", Manager.GetQrCodeForWebUi(key))
                        .WithEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Bai Tosho Web Interface",
                            Url = $"https://dankest.gq/BaiToshoBeta?clientSecret={key}",
                            Description = "Control the bot using a fancy interface.",
                            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                            {
                                Url = "https://dankest.gq/BaiToshoBeta/tosho.png"
                            }
                        }));
                    return;
                }

                var randomString = Bot.RandomString(96);
                await Controllers.Bot.AddUser(ctx.Member.Id, randomString);
                await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent($"```Your Web UI Code is: {randomString}```")
                    .WithFile("qr_code.jpg", Manager.GetQrCodeForWebUi(randomString))
                    .WithEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Bai Tosho Web Interface",
                        Url = $"https://dankest.gq/BaiToshoBeta?clientSecret={randomString}",
                        Description = "Control the bot using a fancy interface.",
                        Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                        {
                            Url = "https://dankest.gq/BaiToshoBeta/tosho.png"
                        }
                    }));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Slash Command: {nameof(GetWebUi)} threw error: {e}");
            }
        }

        [SlashCommand("volume", "This command changes the volume of the bot. Must be between 0 and 200%")]
        public async Task Volume(InteractionContext ctx, [Option("volume", "Volume percent number")] double volume)
        {
            try
            {
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                    return;
                }

                var val = player.UpdateVolume(volume);
                switch (val)
                {
                    case true:
                        await ctx.CreateResponseAsync($"```Set the volume to {volume}%```");
                        break;
                    case false:
                        await ctx.CreateResponseAsync("```Invalid volume range. Must be between 0 and 200%.```");
                        break;
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Slash Command Shuffle failed. {e}");
            }
        }

        [SlashCommand("remove", "This command removes an item from the queue.")]
        public async Task Remove(InteractionContext ctx, [Option("num", "Index to remove")] long num)
        {
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                return;
            }

            var item = player.Queue.RemoveFromQueue((int) num - 1);
            await ctx.CreateResponseAsync($"Removing {item.GetName()}");
        }

        [SlashCommand("saveplaylist", "This command saves the playlist and sends it to the current text channel")]
        public async Task SavePlaylist(InteractionContext ctx)
        {
            try
            {
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync("```You cannot use this command while not being in a channel.```");
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync("```The bot isn't in the channel.```");
                    return;
                }

                var token = $"{ctx.Guild.Id}-{ctx.Channel.Id}-{Bot.RandomString(6)}";
                var fs = SharePlaylist.Write(token, player.Queue.Items);
                fs.Position = 0;
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"```Queue saved sucessfully. \n\nYou can play it again with this command\"-p pl:{token}\", " +
                    "or by sending the attached file and using the play command```").AddFile($"{token}.batp", fs));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Save Playlist Slash Command failed: {e}");
            }
        }

        [SlashCommand("plsfix", "This command makes you pray to the RNG gods.")]
        public async Task PlsFix(InteractionContext ctx)
        {
            try
            {
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync(
                        "```One cannot recieve the blessing of playback if they're not in a channel.```");
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync(
                        "```One cannot recieve the blessing of playback if there's nothing to play.```");
                    return;
                }

                player.PlsFix();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Pls Fix Slash Command failed: {e}");
            }
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Хвани за кура")]
        public async Task CatchDick(ContextMenuContext ctx)
        {
            try
            {
                DiscordMessage respond = null;
                var du = ctx.TargetMember;
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("```Sending request```"));
                if (du.Mention != null)
                {
                    var str = await Methods.ImageMagick.DiscordUserHandler(ctx.User, du, ImageTypes.Dick);
                    //await ctx.RespondAsync($"{ctx.User.Mention} хвана {du.Mention} за кура.");
                    str.Position = 0;
                    await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"```You caught {du.Username}#{du.Discriminator}'s penis.```")
                        .WithFile("hahaha_funny_peepee.jpg", str));
                    str.Position = 0;
                    if (du != ctx.Client.CurrentUser)
                        respond = await du.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent($"```{ctx.User.Username}#{ctx.User.Discriminator} caught your dick.```")
                            .WithFile("hahaha_funny_peepee.jpg", str));
                }

                if (du.IsBot && du.IsCurrent)
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Bot.Clients[0], ":tired_face:"));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Hvani Za Kura Context Menu failed: {e}");
            }
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Send \"Хвани за кура\" request.")]
        public async Task CatchDickRequest(ContextMenuContext ctx)
        {
            try
            {
                DiscordMessage respond = null;
                var du = ctx.TargetMember;
                if (du.Mention != null)
                {
                    const ulong yesEmoji = 837062162471976982;
                    const ulong noEmoji = 837062173296427028;
                    var message =
                        await du.SendMessageAsync(
                            $"```{ctx.User.Username}#{ctx.User.Discriminator} wants you to catch his penis. Do you agree?```");
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("```Sending request```"));
                    if (du.IsBot && du.IsCurrent)
                    {
                        await Task.Delay(3000);
                        respond = await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent($"```You caught \"{du.Username}#{du.Discriminator}\"'s dick.```")
                            .WithFile("hahaha_funny_dick.jpg", await Methods.ImageMagick.DiscordUserHandler
                                (du, ctx.User, ImageTypes.Dick)));
                    }
                    else
                    {
                        await message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Bot.Clients[0], yesEmoji));
                        await message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Bot.Clients[0], noEmoji));
                        //var response = await message.WaitForReactionAsync(du);
                        var timedOut = false;
                        var em = 2;
                        for (var i = 0; i < 38; i++)
                        {
                            var yRec = await message.GetReactionsAsync(
                                DiscordEmoji.FromGuildEmote(Bot.Clients[0], yesEmoji));
                            var nRec = await message.GetReactionsAsync(
                                DiscordEmoji.FromGuildEmote(Bot.Clients[0], noEmoji));
                            if (yRec.Contains(du))
                            {
                                em = 0;
                                break;
                            }

                            if (nRec.Contains(du))
                            {
                                em = 1;
                                break;
                            }

                            await Task.Delay(1200);
                        }

                        if (em == 2) timedOut = true;

                        switch (timedOut)
                        {
                            case false when em is 0:
                                var str = await Methods.ImageMagick.DiscordUserHandler(du, ctx.User, ImageTypes.Dick);
                                str.Position = 0;
                                await ctx.Member.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent($"```You caught \"{du.Username}#{du.Discriminator}\"'s penis.```")
                                    .WithFile("hahaha_funny_peepee.jpg", str));
                                str.Position = 0;
                                respond = await du.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent(
                                        $"```{ctx.Member.Username}#{ctx.Member.Discriminator} caught your penis.```")
                                    .WithFile("hahaha_funny_peepee.jpg", str));
                                break;
                            case true or false when em is 1:
                                await ctx.Member.SendMessageAsync(
                                    $"```You didn't recieve consent from {du.Username}#{du.Discriminator}```");
                                await du.SendMessageAsync("```You didn't give consent```");
                                break;
                            default:
                                return;
                        }
                    }
                }

                if (du.IsBot && du.IsCurrent)
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Bot.Clients[0], ":tired_face:"));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Hvani Me Za Kura Context Menu failed: {e}");
            }
        }
    }
}