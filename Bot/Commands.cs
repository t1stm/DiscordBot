using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Enums;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using BatToshoRESTApp.Tools;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BatToshoRESTApp
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Commands : BaseCommandModule
    {
        private static bool DailySalt { get; set; }

        [Command("vote")]
        public async Task VoteCommand(CommandContext ctx, [RemainingText] string choice)
        {
            try
            {
                await Debug.WriteAsync($"Vote added: {ctx.User.Username}#{ctx.User.Discriminator} - \"{choice}\"", true, Debug.DebugColor.Warning);
                Event.Add($"{ctx.User.Username}#{ctx.User.Discriminator}", choice);
                await ctx.RespondAsync("```Благодаря за вашия глас.```");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Error during vote: \"{e}\"");
            }
        }

        [Command("help")]
        [Aliases("хелп")]
        public async Task HelpCommand(CommandContext ctx, [RemainingText] string command)
        {
            try
            {
                await Manager.SendHelpMessage(ctx.Channel, command);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("play")]
        [Aliases("p", "плаъ", "п")]
        public async Task PlayCommand(CommandContext ctx, [RemainingText] string search)
        {
            try
            {
                await Manager.PlayCommand(ctx, search);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Play command threw exception: {e}");
                throw;
            }
        }

        [Command("skip")]
        [Aliases("next", "скип", "неьт")]
        public async Task SkipCommand(CommandContext ctx, int times = 1)
        {
            try
            {
                await Manager.Skip(ctx, times);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Skip command threw exception: {e}");
                throw;
            }
        }

        [Command("leave")]
        [Aliases("l", "stop", "леаже", "л", "стоп", "с", "s", "die", "дие")]
        public async Task LeaveCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Leave(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Skip command threw exception: {e}");
                throw;
            }
        }

        [Command("shuffle")]
        [Aliases("rand", "схуффле", "ранд")]
        public async Task ShuffleCommand(CommandContext ctx, [RemainingText] string seed)
        {
            try
            {
                if (int.TryParse(seed, out var seedInt))
                {
                    await Debug.WriteAsync("Shuffling using custom seed.");
                    await Manager.Shuffle(ctx, seedInt);
                }

                await Manager.Shuffle(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Shuffle command threw exception: {e}");
                throw;
            }
        }

        [Command("loop")]
        [Aliases("лооп")]
        public async Task Loop(CommandContext ctx)
        {
            try
            {
                await Manager.Loop(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Loop command threw exception: {e}");
                throw;
            }
        }

        [Command("previous")]
        [Aliases("back", "prev", "бацк", "прев", "прежиоус")]
        public async Task PreviousCommand(CommandContext ctx, int times = 1)
        {
            try
            {
                await Manager.Skip(ctx, -times);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Previous command threw exception: {e}");
                throw;
            }
        }

        [Command("pause")]
        [Aliases("паусе")]
        public async Task PauseCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Pause(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Pause command threw exception: {e}");
                throw;
            }
        }

        [Command("playnext")]
        [Aliases("pn", "плаън", "пн")]
        public async Task PlayNextCommand(CommandContext ctx, [RemainingText] string search)
        {
            try
            {
                await Manager.PlayNext(ctx, search);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Play Next command threw exception: {e}");
                throw;
            }
        }

        [Command("playselect")]
        [Aliases("ps")]
        public async Task PlaySelectCommand(CommandContext ctx, [RemainingText] string search)
        {
            try
            {
                await Manager.PlayCommand(ctx, search, true);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Play Select command threw exception: {e}");
                throw;
            }
        }

        [Command("remove")]
        [Aliases("r", "rm", "реможе", "рм", "р")]
        public async Task RemoveCommand(CommandContext ctx, [RemainingText] string remove)
        {
            try
            {
                await Manager.Remove(ctx, remove);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Remove command threw exception: {e}");
                throw;
            }
        }

        [Command("move")]
        [Aliases("m", "mv", "м", "може", "мж")]
        public async Task MoveCommand(CommandContext ctx, [RemainingText] string move)
        {
            try
            {
                await Manager.Move(ctx, move);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Move command threw exception: {e}");
                throw;
            }
        }

        [Command("list")]
        [Aliases("лист", "яуеуе", "queue")]
        public async Task ListCommand(CommandContext ctx)
        {
            try
            {
                await Manager.List(ctx,
                    true); //I plan on making it use a custom site made only for listing the queue, but I will implement it when I make the websocket server.
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"List command threw exception: {e}");
                throw;
            }
        }

        [Command("getavatar")]
        public async Task GetAvatarCommand(CommandContext ctx, DiscordMember user)
        {
            try
            {
                //var us = ctx.Message.MentionedUsers;
                await Bot.Reply(ctx,
                    new DiscordMessageBuilder().WithContent($"```{user.Username}'s avatar```").WithFile(
                        $"{user.Username}.webp",
                        await HttpClient.DownloadStream(user.GetAvatarUrl(ImageFormat.WebP))));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Get avatar command threw exception: {e}");
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
                await Debug.WriteAsync($"Clear command threw exception: {e}");
                throw;
            }
        }

        [Command("getwebui")]
        [Aliases("гетвебуи", "webui", "вебуи", "wu", "ву")]
        public async Task WebUiCommand(CommandContext ctx)
        {
            try
            {
                await Manager.GetWebUi(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Get Web Ui command threw exception: {e}");
                throw;
            }
        }

        [Command("getshuffleseed")]
        public async Task GetShuffleSeedCommand(CommandContext ctx)
        {
            try
            {
                await Manager.GetSeed(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Get Shuffle Seed command threw exception: {e}");
                throw;
            }
        }

        [Command("goto")]
        [Aliases("гото", "go", "го", "skipto", "скипто")]
        public async Task GoToCommand(CommandContext ctx, int index)
        {
            try
            {
                await Manager.GoTo(ctx, index);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Go To command threw exception: {e}");
                throw;
            }
        }

        [Command("saveplaylist")]
        [Aliases("savequeue", "sq", "sp", "сажеяуеуе", "сажеплаълист")]
        public async Task SavePlaylist(CommandContext ctx)
        {
            try
            {
                await Manager.SavePlaylist(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Save playlist to command threw exception: {e}");
                throw;
            }
        }

        [Command("lyrics")]
        public async Task GetLyrics(CommandContext ctx, [RemainingText] string search)
        {
            try
            {
                await Manager.SendLyrics(ctx, search);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Lyrics command threw exception: {e}");
                throw;
            }
        }

        [Command("meme")]
        [Aliases("memes")]
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
                    de.Url = "https://dankest.gq/Bat_Tosho_Content/straight_facts4.png";
                }
                else if (meme != null)
                {
                    de.Url = $"https://dankest.gq/Memes/{meme.Replace("https://dankest.gq/Memes/", "")}";
                }
                else
                {
                    var i = rng.Next(0, memes.Length - 1);
                    if (seenMemes.Length >= memes.Length)
                        seenMemes = new[] {"index.php", "style.css", "latest date.txt"};
                    while (seenMemes.Any(m => m == memes[i])) i = rng.Next(0, memes.Length - 1);

                    meme = memes[i];
                    de.Url = $"https://dankest.gq/Memes/{memes[i].Split("/srv/http/Memes/")[1]}";
                }

                var message = await ctx.RespondAsync(de.Url.Replace(" ", "%20"));
                if (Emojis.CheckIfEmoji(meme, out var emojis))
                    foreach (var emoji in emojis)
                    {
                        await message.CreateReactionAsync(emoji);
                        await Task.Delay(333);
                    }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Meme command threw error: \n{e}");
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
                if (du.Mention != null)
                {
                    var pic = await Methods.ImageMagick.DiscordUserHandler
                        (ctx.User, du, ImageTypes.Dick);
                    pic.Position = 0;
                    //await ctx.RespondAsync($"{ctx.User.Mention} хвана {du.Mention} за кура.");
                    respond = await ctx.RespondAsync(new DiscordMessageBuilder()
                        .WithContent($"{ctx.User.Mention} хвана {du.Mention} за кура.")
                        .WithFile("hahaha_funny_peepee.jpg", pic));
                }

                if (du.IsBot && du.IsCurrent)
                {
                    await ctx.RespondAsync("ohh spicy");
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Bot.Clients[0], ":tired_face:"));
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Hvani za kura command threw error: \n{e}");
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
                if (du.Mention != null)
                {
                    const ulong yesEmoji = 837062162471976982;
                    const ulong noEmoji = 837062173296427028;
                    var message =
                        await ctx.RespondAsync($"{du.Mention} Искаш ли да хванеш {ctx.User.Mention} за кура?");
                    if (du.IsBot && du.IsCurrent)
                    {
                        await Task.Delay(500);
                        await message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(Bot.Clients[0], yesEmoji));
                        await Task.Delay(1000);
                        var pic = await Methods.ImageMagick.DiscordUserHandler
                            (du, ctx.User, ImageTypes.Dick);
                        pic.Position = 0;
                        respond = await ctx.RespondAsync(new DiscordMessageBuilder()
                            .WithContent($"{ctx.User.Mention} беше хванат от {du.Mention} за кура.")
                            .WithFile("hahaha_funny_dick.jpg", pic));
                    }
                    else
                    {
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

                            if (yRec.Contains(du))
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
                                respond = await ctx.RespondAsync(new DiscordMessageBuilder()
                                    .WithContent($"{ctx.User.Mention} беше хванат от {du.Mention} за кура.")
                                    .WithFile("hahaha_funny_peepee.jpg", await Methods.ImageMagick.DiscordUserHandler
                                        (du, ctx.User, ImageTypes.Dick)));
                                break;
                            case true or false when em is 1:
                                await ctx.RespondAsync($"Не бе получен consent от {du.Mention}");
                                break;
                            default:
                                return;
                        }
                    }
                }

                if (du.IsBot && du.IsCurrent)
                {
                    await ctx.RespondAsync("ohh spicy");
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Bot.Clients[0], ":tired_face:"));
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Meme command threw error: \n{e}");
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
                if (du.Mention != null)
                    respond = await ctx.RespondAsync(new DiscordMessageBuilder()
                        .WithContent($"{du.Mention} is now monke.")
                        .WithFile("haha_funny_monke.jpg", await Methods.ImageMagick.DiscordUserHandler
                            (du, null, ImageTypes.Monke)));

                if (du.IsBot && du.IsCurrent)
                {
                    await ctx.RespondAsync("ohh spicy");
                    if (respond != null)
                        await respond.CreateReactionAsync(DiscordEmoji.FromName(Bot.Clients[0], ":tired_face:"));
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Monke command threw error: \n{e}");
                throw;
            }
        }
    }
}