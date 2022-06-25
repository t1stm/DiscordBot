using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Objects;
using MySql.Data.MySqlClient;

namespace DiscordBot.Readers.MariaDB
{
    public class GuildSettings
    {
        public ulong Id { get; init; }
        public ushort Statusbar { get; init; }
        public bool VerboseMessages { get; init; } = true;
        public Languages.Language Language { get; init; }

        private static async Task<List<GuildSettings>> ReadAll()
        {
            var list = new List<GuildSettings>();

            var connection = new MySqlConnection(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var cmd = new MySqlCommand("SELECT * FROM guilds", connection);
            var dataReader = cmd.ExecuteReader();
            while (await dataReader.ReadAsync())
            {
                list.Add(new GuildSettings
                {
                    Id = (ulong) dataReader["id"],
                    Statusbar = (ushort) dataReader["statusbar"],
                    Language = Languages.FromNumber((ushort) dataReader["language"])
                });    
            } 
            await dataReader.CloseAsync(); 
            await connection.CloseAsync();

            return list;
        }

        private static async Task AddGuild(ulong id)
        {
            MySqlConnection connection = new(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var cmd = new MySqlCommand(
                "INSERT INTO guilds (id) " +
                $"VALUES (\"{id}\")", connection);
            cmd.ExecuteNonQuery();
            await connection.CloseAsync();
        }
        
        public static async Task<GuildSettings> FromId(ulong id)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching guild: \"{id}\"");
            var read = await ReadAll();
            var select = read.AsReadOnly().AsParallel().FirstOrDefault(r => r.Id == id);
            if (select != null) return select;
            var settings = new GuildSettings {Id = id};
            await AddGuild(id);
            return settings;
        }
    }
}