using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class GuildsModel : IModel<GuildsModel>
    {
        public ulong Id { get; set; }
        public ushort Language { get; set; }
        public ushort Statusbar { get; set; }
        public bool VerboseMessages { get; set; }
        public bool Normalize { get; set; }
        public bool ShowOriginalInfo { get; set; }
        public bool SaveQueueOnLeave { get; set; }
        public GuildsModel Read(IEnumerable<GuildsModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => r.Id == Id);
        }
    }
}