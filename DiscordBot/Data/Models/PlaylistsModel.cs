#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class PlaylistsModel : Model<PlaylistsModel>
    {
        public string? UserToken { get; set; }
        public List<PlaylistStore> Playlists { get; set; } = Enumerable.Empty<PlaylistStore>().ToList();
        public override void OnLoaded()
        {
            
        }

        public override PlaylistsModel? SearchFrom(IEnumerable<PlaylistsModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => r.Playlists[0].PlaylistId == Playlists[0].PlaylistId);
        }
    }
}