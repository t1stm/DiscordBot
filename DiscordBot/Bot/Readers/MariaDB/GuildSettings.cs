#nullable enable
using System.Threading.Tasks;
using DiscordBot.Data;
using DiscordBot.Data.Models;
using DiscordBot.Methods;

namespace DiscordBot.Readers.MariaDB
{
    // TODO: Move this from the MariaDB namespace.
    public static class GuildSettings
    {
        public static async Task<GuildsModel> FromId(ulong id)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching guild: \"{id}\"");
            var searchModel = new GuildsModel
            {
                Id = id
            };
            return Databases.GuildDatabase.Read(searchModel) ?? Databases.GuildDatabase.Add(searchModel);
        }
    }
}