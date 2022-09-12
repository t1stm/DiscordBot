using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class UsersModel : IModel<UsersModel>
    {
        public ulong Id { get; set; }
        public string Token { get; set; }
        public ushort Language { get; set; }
        public bool UiScroll { get; set; }
        public bool ForceUiScroll { get; set; }
        public bool LowSpec { get; set; }

        public UsersModel Read(IEnumerable<UsersModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => r.Id == Id);
        }
    }
}