using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Audio;
using DiscordBot.Audio.Platforms;
using DiscordBot.Audio.Platforms.Discord;
using DiscordBot.Enums;
using DiscordBot.Methods;
using DiscordBot.Miscellaneous;
using DiscordBot.Objects;
using DiscordBot.Readers.MariaDB;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

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
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            await ctx.CreateResponseAsync(guild.Language.SlashHello().CodeBlocked());
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = //"```You cannot use this command while not being in a channel.```"
                        guild.Language.SlashNotInChannel().CodeBlocked()
                });
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client, generateNew: true);
            if (player == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder
                {
                    Content = guild.Language.NoFreeBotAccounts().CodeBlocked()
                });
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder
            {
                Content = guild.Language.SlashPlayCommand(term).CodeBlocked()
            });
            await Manager.Play(term, false, player, ctx.Member?.VoiceState?.Channel, ctx.Member, null, ctx.Channel);
        }

        [SlashCommand("leave", "This is the leave command. It makes the bot leave.")]
        public async Task LeaveCommand(InteractionContext ctx)
        {
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                return;
            }

            await ctx.CreateResponseAsync(guild.Language.SlashLeaving().CodeBlocked());

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
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                return;
            }

            player.LoopStatus = status switch
            {
                LoopStatus.None => Loop.None, LoopStatus.LoopOne => Loop.One,
                LoopStatus.LoopQueue => Loop.WholeQueue,
                _ => Loop.None
            };

            await ctx.CreateResponseAsync(guild.Language.LoopStatusUpdate(player.LoopStatus).CodeBlocked());
        }

        public async Task MoveCommand(InteractionContext ctx, [Option("item", "The item which you want to move")]
            long x, [Option("place", "The place you want to place the item")]
            long y)
        {
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                return;
            }

            player.Queue.Move((int) x - 1, (int) y - 1, out var item);
            await ctx.CreateResponseAsync(guild.Language.Moved((int) x, item.GetName(), (int) y).CodeBlocked());
        }

        [SlashCommand("skip", "This is the skip command. It makes the bot skip an item.")]
        public async Task SkipCommand(InteractionContext ctx, [Option("times", "Times to skip")] long times = 1)
        {
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                return;
            }

            await ctx.CreateResponseAsync(guild.Language.SlashSkipping((int) times).CodeBlocked());

            await player.Skip((int) times);
        }

        [SlashCommand("pause", "This is the pause command. It pauses the current item.")]
        public async Task PauseCommand(InteractionContext ctx)
        {
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                return;
            }

            await ctx.CreateResponseAsync(guild.Language.SlashPausing().CodeBlocked());
            player.Pause();
        }

        [SlashCommand("shuffle", "This command shuffles the current queue")]
        public async Task Shuffle(InteractionContext ctx)
        {
            try
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                    return;
                }

                player.Shuffle();
                await ctx.CreateResponseAsync(guild.Language.ShufflingTheQueue().CodeBlocked());
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
                var user = await User.FromId(ctx.User.Id);
                await ctx.CreateResponseAsync(user.Language.SendingADirectMessageContainingTheInformation()
                    .CodeBlocked());

                if (!string.IsNullOrEmpty(user.Token))
                {
                    await ctx.Member.SendMessageAsync(Manager.GetWebUiMessage(user.Token,
                        user.Language.YouHaveAlreadyGeneratedAWebUiCode(),
                        user.Language.ControlTheBotUsingAFancyInterface()));
                    return;
                }

                var randomString = Bot.RandomString(96);
                await Controllers.Bot.AddUser(ctx.Member.Id, randomString);
                await ctx.Member.SendMessageAsync(Manager.GetWebUiMessage(randomString, user.Language.YourWebUiCodeIs(),
                    user.Language.ControlTheBotUsingAFancyInterface()));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Slash Command: {nameof(GetWebUi)} threw error: {e}");
            }
        }

        [SlashCommand("volume", "This command changes the volume of the bot. Must be between 0 and 200%")]
        public async Task Volume(InteractionContext ctx, [Option("volume", "Volume percent number")]
            double volume)
        {
            try
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                    return;
                }

                var val = player.UpdateVolume(volume);
                switch (val)
                {
                    case true:
                        await ctx.CreateResponseAsync(guild.Language.SetVolumeTo(volume).CodeBlocked());
                        break;
                    case false:
                        await ctx.CreateResponseAsync(guild.Language.InvalidVolumeRange().CodeBlocked());
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
            var guild = await GuildSettings.FromId(ctx.Guild.Id);
            var userVoiceS = ctx.Member?.VoiceState?.Channel;
            if (userVoiceS == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                return;
            }

            var player = Manager.GetPlayer(userVoiceS, ctx.Client);
            if (player == null)
            {
                await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                return;
            }

            var item = player.Queue.RemoveFromQueue((int) num - 1);
            await ctx.CreateResponseAsync(guild.Language.RemovingItem(item.GetName()).CodeBlocked());
        }

        [SlashCommand("saveplaylist", "This command saves the playlist and sends it to the current text channel")]
        public async Task SavePlaylist(InteractionContext ctx)
        {
            try
            {
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.SlashNotInChannel().CodeBlocked(), true);
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.SlashBotNotInChannel().CodeBlocked(), true);
                    return;
                }

                var token = Manager.GetFreePlaylistToken(ctx.Guild.Id, player.VoiceChannel.Id);

                FileStream fs;
                lock (player.Queue.Items)
                {
                    fs = SharePlaylist.Write(token, player.Queue.Items);
                    fs.Position = 0;
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    guild.Language.QueueSavedSuccessfully(token).CodeBlocked()).AddFile($"{token}.batp", fs));
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
                var guild = await GuildSettings.FromId(ctx.Guild.Id);
                var userVoiceS = ctx.Member?.VoiceState?.Channel;
                if (userVoiceS == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.OneCannotRecieveBlessingNotInChannel().CodeBlocked(),
                        true);
                    return;
                }

                var player = Manager.GetPlayer(userVoiceS, ctx.Client);
                if (player == null)
                {
                    await ctx.CreateResponseAsync(guild.Language.OneCannotRecieveBlessingNothingToPlay().CodeBlocked(),
                        true);
                    return;
                }

                player.PlsFix();
                await ctx.CreateResponseAsync(guild.Language.SlashPrayingToTheRngGods().CodeBlocked());
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

        [SlashCommandGroup("settings", "Change the bot's behavior with this command.")]
        public class CommandsSettings : ApplicationCommandModule
        {
            public enum BooleanEnum
            {
                [ChoiceName("Disabled")] Disabled,
                [ChoiceName("Enabled")] Enabled
            }

            public enum Language
            {
                [ChoiceName("English")] English,
                [ChoiceName("Bulgarian")] Bulgarian
            }

            public enum Verbosity
            {
                [ChoiceName("None")] None,
                [ChoiceName("All")] All
            }

            private static int LanguageParser(Language language)
            {
                return language switch
                {
                    Language.English => 0,
                    Language.Bulgarian => 1,
                    _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
                };
            }

            [SlashCommandGroup("user", "Change the bot's behavior for your account only.")]
            public class UserCommands : ApplicationCommandModule
            {
                [SlashCommand("language", "Change the bot's response language.")]
                public async Task UpdateLanguage(InteractionContext ctx, [Option("language", "Choose a language.")]
                    Language lang)
                {
                    try
                    {
                        var settings = await User.FromId(ctx.User.Id);
                        await settings.ModifySettings("language", $"{LanguageParser(lang)}");
                        await ctx.CreateResponseAsync((lang switch
                        {
                            Language.English => "The bot's responses to you are now in English.",
                            Language.Bulgarian => "Отговорите на бота към теб са вече на Български.",
                            _ => throw new ArgumentOutOfRangeException(nameof(lang), lang, null)
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing user setting \"language\" failed: {e}");
                    }
                }

                [SlashCommand("verboseMessages", "Change the bot's verbosity.")]
                public async Task UpdateVerbosity(InteractionContext ctx,
                    [Option("verbosity", "Choose verbosity level.")]
                    Verbosity verbosity)
                {
                    try
                    {
                        var settings = await User.FromId(ctx.User.Id);
                        await settings.ModifySettings("verboseMessages",
                            $"{verbosity switch {Verbosity.None => 0, Verbosity.All => 1, _ => 1}}");
                        await ctx.CreateResponseAsync((settings.Language switch
                        {
                            English =>
                                $"Changing the verbosity of messages to: \"{verbosity switch {Verbosity.All => "Fully verbose.", Verbosity.None => "Not verbose.", _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, null)}}\"",
                            Bulgarian =>
                                $"Видимостта на отговорите на бота е вече: \"{verbosity switch {Verbosity.All => "Видими отговори.", Verbosity.None => "Скрити отговори.", _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, null)}}\"",
                            _ => throw new ArgumentOutOfRangeException(nameof(settings.Language), settings.Language,
                                "Somehow this value doesn't exist.")
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing user setting \"verboseMessages\" failed: {e}");
                    }
                }

                [SlashCommand("resettoken", "This command resets the token the bot gives you.")]
                public async Task ResetClientToken(InteractionContext ctx)
                {
                    try
                    {
                        var settings = await User.FromId(ctx.User.Id);
                        var token = Bot.RandomString(96);
                        await settings.ModifySettings("token", token);
                        await ctx.CreateResponseAsync(settings.Language.UpdatingToken().CodeBlocked(), true);
                        await ctx.Member.SendMessageAsync(Manager.GetWebUiMessage(token,
                            settings.Language.YourWebUiCodeIs(),
                            settings.Language.ControlTheBotUsingAFancyInterface()));
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync(
                            $"Resetting Client: \"{ctx.User.Username}#{ctx.User.Discriminator} - {ctx.User.Id}\"'s Token failed: \"{e}\"");
                    }
                }
            }

            [SlashCommandGroup("guild", "Change the bot's behavior for the whole guild.")]
            public class GuildCommands : ApplicationCommandModule
            {
                [SlashCommand("language", "Change the bot's response language.")]
                [SlashRequirePermissions(Permissions.Administrator)]
                [SlashRequireGuild]
                public async Task UpdateLanguage(InteractionContext ctx, [Option("language", "Choose a language.")]
                    Language lang)
                {
                    try
                    {
                        var settings = await GuildSettings.FromId(ctx.Guild.Id);
                        await settings.ModifySettings("language", $"{LanguageParser(lang)}");
                        await ctx.CreateResponseAsync((lang switch
                        {
                            Language.English => "The bot's responses to the whole guild are now in English.",
                            Language.Bulgarian => "Отговорите на бота към целия гилд са вече на Български.",
                            _ => throw new ArgumentOutOfRangeException(nameof(lang), lang, null)
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing guild setting \"language\" failed: {e}");
                    }
                }

                [SlashCommand("verboseMessages", "Change the bot's verbosity.")]
                [SlashRequirePermissions(Permissions.Administrator)]
                [SlashRequireGuild]
                public async Task UpdateVerbosity(InteractionContext ctx,
                    [Option("verbosity", "Choose verbosity level.")]
                    Verbosity verbosity)
                {
                    try
                    {
                        var settings = await GuildSettings.FromId(ctx.Guild.Id);
                        await settings.ModifySettings("verboseMessages",
                            $"{verbosity switch {Verbosity.None => 0, Verbosity.All => 1, _ => 1}}");
                        await ctx.CreateResponseAsync((settings.Language switch
                        {
                            English =>
                                $"Changing the verbosity of messages to: \"{verbosity switch {Verbosity.All => "Fully verbose.", Verbosity.None => "Not verbose.", _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, null)}}\"",
                            Bulgarian =>
                                $"Промяна на видимостта на отговорите на бота: \"{verbosity switch {Verbosity.All => "Видими отговори.", Verbosity.None => "Скрити отговори.", _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, null)}}\"",
                            _ => throw new ArgumentOutOfRangeException(nameof(settings.Language), settings.Language,
                                "Somehow this value doesn't exist.")
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing guild setting \"verboseMessages\" failed: {e}");
                    }
                }

                [SlashCommand("normalization", "Change whether the bot should normalize audio.")]
                [SlashRequirePermissions(Permissions.Administrator)]
                [SlashRequireGuild]
                public async Task UpdateNormalization(InteractionContext ctx,
                    [Option("normalization", "Change normalization.")]
                    BooleanEnum booleanEnum)
                {
                    try
                    {
                        var settings = await GuildSettings.FromId(ctx.Guild.Id);
                        await settings.ModifySettings("verboseMessages",
                            $"{booleanEnum switch {BooleanEnum.Disabled => 0, BooleanEnum.Enabled => 1, _ => 1}}");
                        await ctx.CreateResponseAsync((settings.Language switch
                        {
                            English =>
                                $"Changing the normalization of audio to: \"{booleanEnum switch {BooleanEnum.Enabled => "Enabled", BooleanEnum.Disabled => "Disabled", _ => throw new ArgumentOutOfRangeException(nameof(booleanEnum), booleanEnum, null)}}\"",
                            Bulgarian =>
                                $"Нормализирането на звука на бота е вече: \"{booleanEnum switch {BooleanEnum.Enabled => "Включено", BooleanEnum.Disabled => "Изключено", _ => throw new ArgumentOutOfRangeException(nameof(booleanEnum), booleanEnum, null)}}\"",
                            _ => throw new ArgumentOutOfRangeException(nameof(settings.Language), settings.Language,
                                "Somehow this value doesn't exist.")
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing guild setting \"normalization\" failed: {e}");
                    }
                }

                [SlashCommand("showOriginalTitles",
                    "Change whether the bot should show the current item information in it's original language.")]
                [SlashRequirePermissions(Permissions.Administrator)]
                [SlashRequireGuild]
                public async Task ShowOriginalTitles(InteractionContext ctx,
                    [Option("showOriginalTitles", "Change status.")]
                    BooleanEnum booleanEnum)
                {
                    try
                    {
                        var settings = await GuildSettings.FromId(ctx.Guild.Id);
                        await settings.ModifySettings("showOriginalInfo",
                            $"{booleanEnum switch {BooleanEnum.Disabled => 0, BooleanEnum.Enabled => 1, _ => 1}}");
                        await ctx.CreateResponseAsync((settings.Language switch
                        {
                            English =>
                                $"Showing original information is now: \"{booleanEnum switch {BooleanEnum.Enabled => "Enabled", BooleanEnum.Disabled => "Disabled", _ => throw new ArgumentOutOfRangeException(nameof(booleanEnum), booleanEnum, null)}}\"",
                            Bulgarian =>
                                $"Показаната информация е вече: \"{booleanEnum switch {BooleanEnum.Enabled => "На оригиналния език", BooleanEnum.Disabled => "На английски език", _ => throw new ArgumentOutOfRangeException(nameof(booleanEnum), booleanEnum, null)}}\"",
                            _ => throw new ArgumentOutOfRangeException(nameof(settings.Language), settings.Language,
                                "Somehow this value doesn't exist.")
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing guild setting \"normalization\" failed: {e}");
                    }
                }

                [SlashCommand("saveQueueOnLeave",
                    "Change whether the bot should save it's queue after it quits the channel.")]
                [SlashRequirePermissions(Permissions.Administrator)]
                [SlashRequireGuild]
                public async Task SaveQueueOnLeave(InteractionContext ctx,
                    [Option("saveQueueOnLeave", "Change status.")]
                    BooleanEnum booleanEnum)
                {
                    try
                    {
                        var settings = await GuildSettings.FromId(ctx.Guild.Id);
                        await settings.ModifySettings("saveQueueOnLeave",
                            $"{booleanEnum switch {BooleanEnum.Disabled => 0, BooleanEnum.Enabled => 1, _ => 1}}");
                        await ctx.CreateResponseAsync((settings.Language switch
                        {
                            English =>
                                $"Saving queue on leave is now: \"{booleanEnum switch {BooleanEnum.Enabled => "Enabled", BooleanEnum.Disabled => "Disabled", _ => throw new ArgumentOutOfRangeException(nameof(booleanEnum), booleanEnum, null)}}\"",
                            Bulgarian =>
                                $"Ботът вече {booleanEnum switch {BooleanEnum.Enabled => "Запазва списъкът като напуска канала.", BooleanEnum.Disabled => "Не запазва списъкът като напуска канала", _ => throw new ArgumentOutOfRangeException(nameof(booleanEnum), booleanEnum, null)}}",
                            _ => throw new ArgumentOutOfRangeException(nameof(settings.Language), settings.Language,
                                "Somehow this value doesn't exist.")
                        }).CodeBlocked(), true);
                    }
                    catch (Exception e)
                    {
                        await Debug.WriteAsync($"Changing guild setting \"saveQueueOnLeave\" failed: {e}");
                    }
                }
            }
        }
    }
}