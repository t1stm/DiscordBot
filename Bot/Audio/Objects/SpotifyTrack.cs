using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Objects;

namespace DiscordBot.Audio.Objects
{
    public class SpotifyTrack : PlayableItem
    {
        public string TrackId { get; init; }
        public string Album { get; init; }

        public bool Explicit { get; init; }

        public override Task Download()
        {
            return Task.CompletedTask;
        }

        public override string GetId()
        {
            return "";
        }

        public override string GetThumbnailUrl()
        {
            return "";
        }

        public override string GetAddUrl()
        {
            return $"spt://{TrackId}";
        }
    }
}