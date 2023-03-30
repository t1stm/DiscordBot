#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DatabaseManager;

namespace DiscordBot.Data.Models;

public class VotesModel : Model<VotesModel>
{
    [JsonInclude] public ulong UserId { get; set; }

    [JsonInclude] public string Choice { get; set; } = null!;

    public override VotesModel? SearchFrom(IEnumerable<VotesModel> source)
    {
        return source.AsParallel().FirstOrDefault(r => UserId == r.UserId);
    }
}