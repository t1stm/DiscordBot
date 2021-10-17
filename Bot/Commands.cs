using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bat_Tosho.Audio;
using Bat_Tosho.Enums;
using Bat_Tosho.Messages;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;
using DSharpPlus.VoiceNext;
using Swan;

namespace Bat_Tosho
{
    public class Commands : BaseCommandModule
    {
        public static bool Playing { get; set; }
        private static bool DailySalt { get; set; }

        private static Dictionary<ulong, bool> Abuse { get; } = new();

        private static async Task MultipleCommandsChecker(string text, CommandContext ctx)
        {
            var commands = text[1..].Split("!&&"); // nice
            // Quick explanation: This is a range indexer. It really is useful in cases like these. It can be used in an array too, and technically a string is a char array.
            // It is used like this: [startIndex..EndIndex]. Both the start and end indexes can be omitted.
            foreach (var t in commands)
            {
                string s;
                s = (t[0] is '=' or '-' or ';') switch {true => t[1..], false => t};
                switch (s.Trim().Split(" ").First().Trim())
                {
                    case "shuffle":
                        await Debug.Write($"t: \"{t.Trim()}\"");
                        await Manager.Shuffle(ctx);
                        break;
                    case "skip" or "next":
                        await Debug.Write($"t: \"{t.Trim()}\"");
                        await Manager.Skip(ctx);
                        break;
                    case "previous" or "back":
                        await Manager.Skip(ctx, -1);
                        break;
                    case "play":
                        var playTask = new Task(async () =>
                        {
                            try
                            {
                                await Manager.Play(ctx, t[4..]); //The same thing here.
                            }
                            catch (Exception e)
                            {
                                await Debug.Write($"Play Command Failed. {e}");
                            }
                        });
                        playTask.Start();
                        while (!Manager.ExecutedCommand) await Task.Delay(333);

                        Manager.ExecutedCommand = false;
                        break;
                }

                await Task.Delay(333);
            }
        }

        [Command("play")]
        [Aliases("p", "п", "плаъ", "udri", "удри", "playfile")]
        [Description("The play command. Simple right?")]
        private async Task PlayCommand(CommandContext ctx, [RemainingText] string path)
        {
            try
            {
                var text = ctx.Message.Content.ToLower().StartsWith($"{Program.BotPrefixes}п") switch
                {
                    true => Translate.BulgarianTraditionalToQwerty(path),
                    false => path
                };
                await MultipleCommandsChecker($"=play {text}", ctx);
            }
            catch (Exception e) 
                // Ahh yes the try, catch spam initiates. Gotta not crash the bot when the spaghetti code acts up.
                // Update: it still crashes...
            {
                await Debug.Write($"Play command threw error: {e}");
            }
        }

        [Command("leave")]
        [Aliases("l", "л", "stop", "s", "die", "стоп", "дие", "леаже")]
        [Description("This command makes the bot leave like your father did for milk 10 years ago.")]
        public async Task LeaveCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Leave(ctx);
            }
            catch (Exception e)
            {
                await Debug.Write($"Leave command threw error: {e}");
            }
        }

        [Command("skip")]
        [Aliases("next", "скип", "неьт")]
        [Description("This command skips to the next track.")]
        public async Task SkipCommand(CommandContext ctx, int times = 1)
        {
            try
            {
                await Manager.Skip(ctx, times);
            }
            catch (Exception e)
            {
                await Debug.Write($"Skip command threw error: {e}");
            }
        }

        [Command("previous")]
        [Aliases("back", "return", "прежиоус", "бацк")]
        [Description("This command skips to the next track.")]
        public async Task PreviousCommand(CommandContext ctx, int times = 1)
        {
            try
            {
                await Manager.Skip(ctx, -times);
            }
            catch (Exception e)
            {
                await Debug.Write($"Previous command threw error: {e}");
            }
        }

        [Command("list")]
        [Aliases("ls", "лист", "лс", "queue", "яуеуе")]
        [Description("Lists the current queue.")]
        public async Task ListCommand(CommandContext ctx)
        {
            try
            {
                await Manager.List(ctx);
            }
            catch (Exception e)
            {
                await Debug.Write($"List command threw error: {e}");
            }
        }

        [Command("shuffle")]
        [Aliases("shuf", "схуффле")]
        [Description("Shuffles the playlist.")]
        public async Task ShuffleCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Shuffle(ctx);
            }
            catch (Exception e)
            {
                await Debug.Write($"Shuffle command threw error: {e}");
            }
        }

        [Command("loop")]
        [Aliases("repeat")]
        public async Task LoopCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Loop(ctx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Command("download")]
        [Aliases("dll", "довнлоад", "длл")]
        [Description("Downloads a youtube video and links it.")]
        public async Task DownloadCommand(CommandContext ctx, [RemainingText] string path)
        {
            try
            {
                await Manager.Download(ctx, path);
            }
            catch (Exception e)
            {
                await Debug.Write($"Download command threw error: {e}");
            }
        }

        [Command("remove")]
        [Description(
            "The remove command. No, it can't remove your grandmother's cancer, but it can remove a song from the queue.")]
        [Aliases("r", "rem", "rm", "реможе", "р", "рм", "рем")]
        public async Task RemoveCommand(CommandContext ctx,
            [Description(@"Pass a song number obtained using =list. Usage: ""=remove n"" where n is a number")]
            int index)
        {
            try
            {
                await Manager.Remove(ctx, index - 1);
            }
            catch (Exception e)
            {
                await Debug.Write($"Remove command threw error: \n{e}");
                throw;
            }
        }

        [Command("move")]
        [Description(
            "The move command. No, it can't move your home to Africa, where you belong, but it can move a song from the queue.\n" +
            @"Usage: ""=move x y"" where x and y are indexes of the old place and new place repectively")]
        [Aliases("mv", "може", "мж")]
        public async Task MoveCommand(CommandContext ctx, [Description("Old Location Index")] int oldIndex,
            [Description("New Location Index")] int newIndex)
        {
            try
            {
                await Manager.Move(ctx, oldIndex, newIndex);
            }
            catch (Exception e)
            {
                await Debug.Write($"Move command threw error: \n{e}");
                throw;
            }
        }

        [Command("pause")]
        [Description("Pauses the current song.")]
        [Aliases("паусе")]
        public async Task PauseCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Pause(ctx);
            }
            catch (Exception e)
            {
                await Debug.Write($"Pause command threw error: \n{e}");
                throw;
            }
        }

        [Command("clear")]
        public async Task ClearCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Clear(ctx);
            }
            catch (Exception e)
            {
                await Debug.Write($"Clear Command Failed: {e}");
                throw;
            }
        }

        [Command("playnext")]
        [Aliases("pn", "пн", "плаънеьт")]
        public async Task PlaynextTask(CommandContext ctx, [RemainingText] string path)
        {
            try
            {
                await Manager.PlayNext(ctx, path);
            }
            catch (Exception e)
            {
                await Debug.Write($"Volume Command Failed: {e}");
                throw;
            }
        }

        [Command("webui")]
        public async Task WebUiCommand(CommandContext ctx)
        {
            try
            {
                await Manager.GetWebUi(ctx);
            }
            catch (Exception e)
            {
                await Debug.Write($"Web UI Command Failed: {e}");
            }
        }

        [Command("abuse")]
        public async Task AbuseThePerson(CommandContext ctx, DiscordMember member)
        {
            try
            {
                if (!Abuse.ContainsKey(ctx.Guild.Id)) Abuse.Add(ctx.Guild.Id, false);
                Abuse[ctx.Guild.Id] = !Abuse[ctx.Guild.Id];
                if (!Abuse[ctx.Guild.Id])
                {
                    Abuse.Remove(ctx.Guild.Id);
                    return;
                }

                if (ctx.User.Id != ctx.Guild.OwnerId) await ctx.RespondAsync("```Nice try!```");
                while (Abuse[ctx.Guild.Id])
                {
                    var target = await ctx.Guild.GetMemberAsync(member.Id);
                    if (target.VoiceState.Channel != null)
                        await target.ModifyAsync(delegate(MemberEditModel model) { model.VoiceChannel = null; });
                    await Task.Delay(1200);
                }
            }
            catch (Exception)
            {
                await Task.CompletedTask;
            }
        }

        [Command("volume")]
        [Description("Earrape Intensifies.")]
        [Aliases("vol", "v", "жолуме", "ж", "жол")]
        public async Task VolumeTask(CommandContext ctx, double volume)
        {
            try
            {
                await Manager.SetVolume(ctx, volume);
            }
            catch (Exception e)
            {
                await Debug.Write($"Volume Command Failed: {e}");
                throw;
            }
        }

        [Command("lyrics")]
        [Aliases("лърицс")]
        public async Task LyricsTask(CommandContext ctx, [RemainingText] string text)
        {
            try
            {
                await Manager.Lyrics(ctx, text);
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case InvalidOperationException:
                        await Respond.FormattedMessage(ctx, "Couldn't find the lyrics for the song.");
                        break;
                    case InvalidDataException:
                        await Respond.FormattedMessage(ctx,
                            "The lyrics of the song are too long for Discord to handle. Sorry.");
                        break;
                    default:
                        await Debug.Write($"Lyrics command failed: {e}");
                        throw;
                }
            }
        }

        [Command("ungag")]
        public async Task UngagCommand(CommandContext ctx, DiscordUser target)
        {
            try
            {
                await Debug.Write("Ungag command started.");
                await Manager.Ungag(ctx, target);
            }
            catch (Exception e)
            {
                await Debug.Write($"Ungag command failed: {e}");
                throw;
            }
        }

        [Command("UpdateDefaultStatus")]
        public async Task UpdateStatusCommand(CommandContext ctx, string activity, string activityName,
            string url = null)
        {
            try
            {
                await ctx.Message.CreateReactionAsync(Emojis.PlayEmoji);
                Program.IdleActivity = activity.ToLower() switch
                {
                    "listening to" => ActivityType.ListeningTo,
                    "watching" => ActivityType.Watching,
                    "playing" => ActivityType.Playing,
                    "competing in" => ActivityType.Competing,
                    "streaming" => ActivityType.Streaming,
                    _ => Program.IdleActivity
                };
                Program.IdleStatus = activityName;
                Program.DiscordActivity.Name = Program.IdleStatus;
                Program.DiscordActivity.ActivityType = Program.IdleActivity;
                if (!string.IsNullOrEmpty(url))
                    Program.DiscordActivity.StreamUrl = url;
                await Program.Discord.UpdateStatusAsync(Program.DiscordActivity);
                await Respond.FormattedMessage(ctx, $@"New Status is {activity} {activityName} {url}.");
            }
            catch (Exception e)
            {
                await Debug.Write($"Updating Default Status failed: {e}");
                throw;
            }
        }

        [Command("bible")]
        public async Task Bible(CommandContext ctx)
        {
            await ctx.Message.CreateReactionAsync(Emojis.PlayEmoji);
            await Respond.FormattedMessage(ctx, "Currently Playing The Whole Bible in Hebrew");
            await PlayCommand(ctx, "https://www.youtube.com/watch?v=iEXOlP-6WkQ"); // kekw easy solution
        }

        [Command("meinkampf")]
        [Aliases("germanyisnumberone", "hailhitler")]
        public async Task MeinKampf(CommandContext ctx)
        {
            await ctx.Message.CreateReactionAsync(Emojis.PlayEmoji);
            var oldActivity = new DiscordActivity(Program.DiscordActivity.Name, Program.DiscordActivity.ActivityType);
            await Respond.FormattedMessage(ctx, "Currently Playing Whole Mein Kampf in English (Bat Tosho Exclusive)");
            await ctx.Member.VoiceState.Channel.ConnectAsync();
            Program.DiscordActivity.Name = "Mein Kampf - Adolf Hitler";
            Program.DiscordActivity.ActivityType = ActivityType.ListeningTo;
            await PlayCommand(ctx, "https://www.youtube.com/watch?v=a2C90l7YlT8");
            Program.DiscordActivity = oldActivity;
            await Program.Discord.UpdateStatusAsync(Program.DiscordActivity);
        }

        [Command("meme")]
        [Aliases("m", "memes", "м")]
        public async Task MemeCommand(CommandContext ctx, [RemainingText] string meme = null)
        {
            try
            {
                var de = new DiscordEmbedBuilder();
                var rng = new Random();
                string[] seenMemes = {"index.php", "style.css", "latest date.txt"};
                var memes = Directory.GetFiles("/srv/http/Memes");
                if (!DailySalt)
                {
                    DailySalt = true;
                    de.Url = "https://dank.gq/Bat_Tosho_Content/straight_facts4.png";
                }
                else if (meme != null)
                {
                    de.Url = $"https://dank.gq/Memes/{meme.Replace("http://dank.gq/Memes/", "")}";
                }
                else
                {
                    var i = rng.Next(0, memes.Length - 1);
                    if (seenMemes.Length >= memes.Length)
                        seenMemes = new[] {"index.php", "style.css", "latest date.txt"};
                    while (CheckIfExistsInStringArray(memes[i], seenMemes)) rng.Next(0, memes.Length - 1);

                    meme = memes[i];
                    de.Url = $"https://dank.gq/Memes/{memes[i].Split("/srv/http/Memes/")[1]}";
                }

                var message = await ctx.RespondAsync(de.Url.Replace(" ", "%20"));
                if (Emojis.CheckIfEmoji(meme))
                    foreach (var emoji in Emojis.EmojiToBeUsed)
                    {
                        await message.CreateReactionAsync(emoji);
                        await Task.Delay(333);
                    }
            }
            catch (Exception e)
            {
                await Debug.Write($"Meme command threw error: \n{e}");
                throw;
            }
        }

        [Command("hvanizakura")]
        [Aliases("хванизакура")]
        public async Task HvaniZaKura(CommandContext ctx, DiscordUser du)
        {
            try
            {
                DiscordMessage respond = null;
                du ??= ctx.User;
                var im = new Methods.ImageMagick();
                if (du.Mention != null)
                {
                    await ctx.RespondAsync($"{ctx.User.Mention} хвана {du.Mention} за кура.");
                    respond = await ctx.RespondAsync(
                        $"{await im.DiscordUserHandler(ctx.User, du, ImageTypes.Dick)}");
                }

                if (du.IsBot && du.IsCurrent)
                {
                    await ctx.RespondAsync("ohh spicy");
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Program.Discord, ":tired_face:"));
                }
            }
            catch (Exception e)
            {
                await Debug.Write($"Hvani za kura command threw error: \n{e}");
                throw;
            }
        }

        [Command("hvanimezakura")]
        [Aliases("хванимезакура")]
        public async Task HvaniMeZaKura(CommandContext ctx, DiscordUser du = null)
        {
            try
            {
                DiscordMessage respond = null;
                du ??= ctx.User;
                var im = new Methods.ImageMagick();
                if (du.Mention != null)
                {
                    const ulong yesEmoji = 837062162471976982;
                    const ulong noEmoji = 837062173296427028;
                    if (ctx.User.Username == "TITO") //No Consent because legend.
                    {
                        await ctx.RespondAsync($"{ctx.User.Mention} беше хванат от {du.Mention} за кура.");
                        respond = await ctx.RespondAsync(
                            $"{await im.DiscordUserHandler(du, ctx.User, ImageTypes.Dick)}");
                    }
                    else //Ask for consent
                    {
                        var message =
                            await ctx.RespondAsync($"{du.Mention} Искаш ли да хванеш {ctx.User.Mention} за кура?");
                        if (du.IsBot && du.IsCurrent)
                        {
                            await Task.Delay(500);
                            await message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Program.Discord, yesEmoji));
                            await Task.Delay(1000);
                            await ctx.RespondAsync($"{ctx.User.Mention} беше хванат от {du.Mention} за кура.");
                            respond = await ctx.RespondAsync(
                                $"{await im.DiscordUserHandler(du, ctx.User, ImageTypes.Dick)}");
                        }
                        else
                        {
                            await message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Program.Discord, yesEmoji));
                            await message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Program.Discord, noEmoji));
                            var response = await message.WaitForReactionAsync(du);
                            switch (response.TimedOut)
                            {
                                case false when response.Result.Emoji.Equals(
                                    DiscordEmoji.FromGuildEmote(Program.Discord, yesEmoji)):
                                    await ctx.RespondAsync($"{ctx.User.Mention} беше хванат от {du.Mention} за кура.");
                                    respond = await ctx.RespondAsync(
                                        $"{await im.DiscordUserHandler(du, ctx.User, ImageTypes.Dick)}");
                                    break;
                                case false when response.Result.Emoji.Equals(
                                    DiscordEmoji.FromGuildEmote(Program.Discord, noEmoji)):
                                    await ctx.RespondAsync($"Не бе получен consent от {du.Mention}");
                                    break;
                                default:
                                    return;
                            }
                        }
                    }
                }

                if (du.IsBot && du.IsCurrent)
                {
                    await ctx.RespondAsync("ohh spicy");
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Program.Discord, ":tired_face:"));
                }
            }
            catch (Exception e)
            {
                await Debug.Write($"Meme command threw error: \n{e}");
                throw;
            }
        }

        [Command("monke")]
        public async Task MonkeCommand(CommandContext ctx, DiscordUser du = null)
        {
            try
            {
                DiscordMessage respond = null;
                du = du switch
                {
                    null => ctx.User,
                    _ => du
                };
                var im = new Methods.ImageMagick();
                if (du.Mention != null)
                {
                    await ctx.RespondAsync($"{du.Mention} is now monke.");
                    respond = await ctx.RespondAsync(
                        $"{await im.DiscordUserHandler(du, null, ImageTypes.Monke)}");
                }

                if (du.IsBot && du.IsCurrent)
                {
                    await ctx.RespondAsync("ohh spicy");
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Program.Discord, ":tired_face:"));
                }
            }
            catch (Exception e)
            {
                await Debug.Write($"Monke command threw error: \n{e}");
                throw;
            }
        }

        [Command("getavatar")]
        public async Task GetUserAvatar(CommandContext ctx, DiscordUser du = null)
        {
            var userAvatar = du?.AvatarUrl ?? ctx.User.AvatarUrl;
            await ctx.RespondAsync(userAvatar);
        }

        [Command("getusername")]
        public async Task GetUsername(CommandContext ctx, DiscordUser du)
        {
            var userName = du.Username;
            await ctx.RespondAsync(userName);
        }

        [Command("getstatus")]
        public async Task GetStatus(CommandContext ctx, DiscordMember dm)
        {
            try
            {
                var member = await ctx.Guild.GetMemberAsync(dm.Id);
                //var activity = member.Presence.Activities;
                //var activities = activity.Aggregate("", (current, a) => current + $"Activity: Name:{a.Name}, Type: {a.ActivityType}\n" + $"Rich Presence: Application: {a.RichPresence.Application}, Details: {a.RichPresence.Details}, State: {a.RichPresence.State}, Join Secret: {a.RichPresence.JoinSecret}");
                //await Respond.FormattedMessage(ctx, $"Activities are: {activities}");
                await Debug.Write($"```Member: \n{member.ToJson()} ```");
            }
            catch (Exception e)
            {
                await Debug.Write($"Get Status command threw error: \n{e}");
                throw;
            }
        }

        [Command("kanyewestquote")]
        [Aliases("kwquote", "kwq")]
        [Description("If you're reading this description, you're probably wondering why I added this command. " +
                     "To put it simply: I found an api and I implemented it into the bot.")]
        public async Task KanyeQuoteCommand(CommandContext ctx)
        {
            try
            {
                var quote = await new WebClient().DownloadStringTaskAsync("https://api.kanye.rest/");
                if (quote.Length > 10) quote = quote[10..^2];
                await Respond.FormattedMessage(ctx, $"Kanye West: \n\"{quote}\"");
            }
            catch (Exception e)
            {
                await Debug.Write($"The fucking Kanye West Quote Command Broke: {e}");
                throw;
            }
        }

        [Command("gayrate")]
        public async Task GayRateMachine(CommandContext ctx, DiscordUser du = null)
        {
            try
            {
                if (ctx.Message.MessageType == MessageType.Reply &&
                    ctx.Message.MentionedUsers.Contains(ctx.Client.CurrentUser)) return;
                if (ctx.User.IsBot && ctx.User.IsCurrent)
                    return;
                if (du == null && ctx.Message.MentionedUsers.Count == 0)
                    du = ctx.User;
                else if (ctx.Message.MentionedUsers.Count > 0) du = ctx.Message.MentionedUsers.First();

                if (du.IsCurrent)
                {
                    await ctx.TriggerTypingAsync();
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Program.Discord,
                        841685118661033985));
                }
                else if (du.IsBot /*&& du.Username is "Dank Memer" or "Groovy"*/)
                {
                    await ctx.RespondAsync("** the bettr gay r8 machine \n" +
                                           $"{du.Mention} is {Program.Rng.Next(0, 100) * 5}% gay. **");
                }
                else
                {
                    await ctx.RespondAsync("** the bettr gay r8 machine \n" +
                                           $"{du.Mention} is {Program.Rng.Next(0, 100)}% gay. **");
                }
            }
            catch (Exception e)
            {
                await Debug.Write($"Gayrate command threw error: \n{e}");
                throw;
            }
        }

        private static bool CheckIfExistsInStringArray(string contents, IEnumerable<string> array)
        {
            return array.Contains(contents);
        }
    }
}