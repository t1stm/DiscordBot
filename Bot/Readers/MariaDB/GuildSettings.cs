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
        public ILanguage Language { get; init; }
        public bool Normalize { get; private init; } = true;

        public async Task ModifySettings(string target, string value) // I am not going to bother making this method safe.
        {
            var connection = new MySqlConnection(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var cmd = new MySqlCommand($"UPDATE `guilds` SET `{target}` = '{value}' WHERE `guilds`.`id` = '{Id}'", connection);
            await cmd.ExecuteNonQueryAsync();
            await connection.CloseAsync();
        }

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
                    Language = Parser.FromNumber((ushort) dataReader["language"]),
                    VerboseMessages = (bool) dataReader["verboseMessages"],
                    Normalize = (bool) dataReader["normalize"]
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