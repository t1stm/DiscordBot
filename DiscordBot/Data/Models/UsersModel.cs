#nullable enable
using System.Collections.Generic;
using System.Linq;
using DatabaseManager;

namespace DiscordBot.Data.Models;

public class UsersModel : Model<UsersModel>
{
    public ulong Id { get; set; }
    public string? Token { get; set; }
    public ushort Language { get; set; }
    public bool UiScroll { get; set; } = true;
    public bool VerboseMessages { get; set; } = true;
    public bool ForceUiScroll { get; set; }
    public bool LowSpec { get; set; }

    public override UsersModel? SearchFrom(IEnumerable<UsersModel> source)
    {
        return source.AsParallel().FirstOrDefault(r => r.Id == Id || r.Token == Token);
    }
}