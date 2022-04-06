using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;

namespace BatToshoRESTApp.Audio.Objects
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

        public override string GetTypeOf()
        {
            return "Spotify Track";
        }

        public override string GetThumbnailUrl()
        {
            return "";
        }
    }
}