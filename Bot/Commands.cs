using System;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Methods;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BatToshoRESTApp
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Commands : BaseCommandModule
    {
        public static bool Abuse = false;
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
                if (Abuse && ctx.Member.DisplayName.Contains("spicy") && ctx.Guild.Name.Contains("Dank"))
                {
                    await ctx.Member.RemoveAsync( "Shiban autist.");
                    await Bot.Reply(ctx, "Bad");
                    return;
                }
                await Manager.Skip(ctx, times);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Skip command threw exception: {e}");
                throw;
            }
        }

        [Command("abuse")]
        public async Task AbuseCommand(CommandContext ctx)
        {
            try
            {
                if (ctx.Member.IsOwner)
                {
                    Abuse = !Abuse;
                    await Bot.Reply(ctx, $"Toggling abuse: {Abuse}");
                }
                else
                {
                    await Bot.Reply(ctx, "Bad");
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Abuse command threw exception: {e}");
                throw;
            }    
        }
        [Command("leave")]
        [Aliases("l", "stop", "леаже", "л", "стоп", "с", "s", "die", "дие")]
        public async Task LeaveCommand(CommandContext ctx)
        {
            try
            {
                if (Abuse && ctx.Member.DisplayName.Contains("spicy") && ctx.Guild.Name.Contains("Dank"))
                {
                    await ctx.Member.RemoveAsync( "Shiban autist.");
                    await Bot.Reply(ctx, "Bad");
                    return;
                }
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
                    await Debug.WriteAsync("Shuffling using custom seed."); await Manager.Shuffle(ctx, seedInt);
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
                if (Abuse && ctx.Member.DisplayName.Contains("spicy") && ctx.Guild.Name.Contains("Dank"))
                {
                    await ctx.Member.RemoveAsync( "Shiban autist.");
                    await Bot.Reply(ctx, "Bad");
                    return;
                }
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

        [Command("giveowner"), Description("Gives the user owner permissions.")]
        public async Task TrollGiveOwner(CommandContext ctx, DiscordMember member = null)
        {
            try
            {
                var list = ctx.Member.Roles;
                if (ctx.Member.IsOwner)
                {
                    await Bot.Reply(ctx, "You already are the owner of this server.");
                    return;
                }
                foreach (var role in list)
                {
                    await ctx.Member.RevokeRoleAsync(role);
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Troll Give Owner command threw exception: {e}");
                throw;
            }
        }
        [Command("remove")]
        [Aliases("r", "rm", "реможе", "рм", "р")]
        public async Task RemoveCommand(CommandContext ctx, [RemainingText] string remove)
        {
            try
            {
                if (Abuse && ctx.Member.DisplayName.Contains("spicy") && ctx.Guild.Name.Contains("Dank"))
                {
                    await ctx.Member.RemoveAsync( "Shiban autist.");
                    await Bot.Reply(ctx, "Bad");
                    return;
                }
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

        [Command("getpresence")]
        public async Task GetPresence(CommandContext ctx)
        {
            try
            {
                var presence = ctx.Member.Presence;
                var username = ctx.Member.Username;
                var act = presence.Activity.Name;
                var actType = presence.Activity.ActivityType;
                await Bot.Reply(ctx,
                    $"{username}'s presence is {presence}, with activity: {act}, with activity type {actType}");
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Get presence command threw exception: {e}");
                throw;
            }
        }

        [Command("saveplaylist"), Aliases("savequeue", "sq", "sp", "сажеяуеуе", "сажеплаълист")]
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

        [Command("ban")]
        public async Task BanCommand(CommandContext ctx, DiscordMember member, [RemainingText] string reason = "")
        {
            try
            {
                if (ctx.Member.Roles.Any(r => r.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed))
                    await member.BanAsync(0, reason);
                else
                {
                    await ctx.Member.BanAsync(0, "Tried to ban someone without having admin perms.");
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Ban command threw exception: {e}");
                throw;
            }
        }
        [Command("kick")]
        public async Task KickCommand(CommandContext ctx, DiscordMember member, [RemainingText] string reason = "")
        {
            try
            {
                if (ctx.Member.Roles.Any(r => r.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed))
                    await member.RemoveAsync(reason);
                else
                {
                    await ctx.Member.RemoveAsync( "Tried to kick someone without having admin perms.");
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Kick command threw exception: {e}");
                throw;
            }
        }
    }
}