using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Bat_Tosho.Messages
{
    public static class Respond
    {
        public static async Task<DiscordMessage> FormattedMessage(CommandContext ctx, string message)
        {
            return await ctx.Message.RespondAsync($"```{message}```");
        }
    }
}