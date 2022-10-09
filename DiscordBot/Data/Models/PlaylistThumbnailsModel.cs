#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class PlaylistThumbnailsModel: Model<PlaylistThumbnailsModel>
    {
        public string Id { get; init; } = null!;
        public bool Expired { get; set; }
        public override PlaylistThumbnailsModel? SearchFrom(IEnumerable<PlaylistThumbnailsModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => r.Id == Id);
        }
    }
}