using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Readers.MariaDB;
using MySql.Data.MySqlClient;

namespace DiscordBot.Objects
{
    public class User
    {
        public ulong Id { get; init; }
        public string Token { get; init; }
        public bool VerboseMessages { get; init; } = true;
        public ILanguage Language { get; init; }

        public async Task ModifySettings(string target, string value)
        {
            var connection = new MySqlConnection(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var request = $"UPDATE `users` SET `{target}` = '{value}' WHERE `users`.`id` = '{Id}'";
            if (Bot.DebugMode) await Debug.WriteAsync($"Updating user with id: ({Id}): \"{request}\"");
            var cmd = new MySqlCommand(request, connection);
            await cmd.ExecuteNonQueryAsync();
            await connection.CloseAsync();
        }
        
        public static async Task<User> FromId(ulong id)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching user: \"{id}\"");
            var read = await ClientTokens.ReadAll();
            var select = read.AsReadOnly().AsParallel().FirstOrDefault(r => r.Id == id);
            if (select != null)
            {
                if (Bot.DebugMode) await Debug.WriteAsync($"Returning found user: \"{id}\", {select.VerboseMessages}, {select.Language}");
                return select;
            }
            var user = new User
            {
                Id = id
            };

            await ClientTokens.Add(id);
            return user;
        }
    }
}