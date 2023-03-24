#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models;

public class GuildsModel : Model<GuildsModel>
{
    public ulong Id { get; set; }
    public ushort Language { get; set; }
    public ushort Statusbar { get; set; }
    public bool VerboseMessages { get; set; } = true;
    public bool Normalize { get; set; } = true;
    public bool ShowOriginalInfo { get; set; }
    public bool SaveQueueOnLeave { get; set; } = true;

    public override GuildsModel? SearchFrom(IEnumerable<GuildsModel> source)
    {
        return source.AsParallel().FirstOrDefault(r => r.Id == Id);
    }
}