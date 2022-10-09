#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class PlaylistsModel : Model<PlaylistsModel>
    {
        public string[] PlaylistIds { get; set; } = Array.Empty<string>();
        public string? UserToken { get; set; }
        
        public override PlaylistsModel? SearchFrom(IEnumerable<PlaylistsModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => r.PlaylistIds[0] == PlaylistIds[0]);
        }
    }
}