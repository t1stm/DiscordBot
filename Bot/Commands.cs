using System;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio;
using BatToshoRESTApp.Methods;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BatToshoRESTApp
{
    public class Commands : BaseCommandModule
    {
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
        [Aliases("l", "s", "леаже", "л", "с")]
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
        public async Task ShuffleCommand(CommandContext ctx)
        {
            try
            {
                await Manager.Shuffle(ctx);
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Shuffle command threw exception: {e}");
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
    }
}