using System;

namespace DiscordBot.Data.Models
{
    public class PlaylistStore
    {
        public string PlaylistId { get; set; } = Guid.NewGuid().ToString();
        public Thumbnail Thumbnail { get; set; } = new()
        {
            Expired = true
        };
    }
    public class Thumbnail
    {
        public bool Expired { get; set; }
    }
}