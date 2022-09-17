using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class UsersModel : IModel<UsersModel>
    {
        public ulong Id { get; set; }
        public string Token { get; set; } = null;
        public ushort Language { get; set; } = 0;
        public bool UiScroll { get; set; } = true;
        public bool VerboseMessages { get; set; } = true;
        public bool ForceUiScroll { get; set; }
        public bool LowSpec { get; set; }

        public UsersModel Read(IEnumerable<UsersModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => r.Id == Id || r.Token == Token);
        }
    }
}