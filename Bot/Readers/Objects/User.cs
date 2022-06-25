using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Readers.MariaDB;

namespace DiscordBot.Objects
{
    public class User
    {
        public ulong Id { get; init; }
        public string Token { get; init; }
        public bool VerboseMessages { get; init; } = true;
        public Languages.Language Language { get; init; }
        
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