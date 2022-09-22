#nullable enable
using System.Threading.Tasks;
using DiscordBot.Data;
using DiscordBot.Data.Models;
using DiscordBot.Methods;

namespace DiscordBot.Readers.MariaDB
{
    public class GuildSettings
    {
        public static async Task<GuildsModel> FromId(ulong id)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching guild: \"{id}\"");
            var searchModel = new GuildsModel
            {
                Id = id
            };
            var select = Databases.GuildDatabase.Read(searchModel);
            if (select != null) return select;
            Databases.GuildDatabase.Add(searchModel);
            return searchModel;
        }
    }
}