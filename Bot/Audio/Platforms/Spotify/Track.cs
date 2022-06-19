using System.Threading.Tasks;
using DiscordBot.Audio.Objects;
using SpotifyAPI.Web;

namespace DiscordBot.Audio.Platforms.Spotify
{
    public static class Track
    {
        private static readonly SpotifyClientConfig SpotifyConfig = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator
                ("e90ef778e6e14879a9355acd51a78c2d", "2538249a5e68467e8b3e997c3a1774d2"));

        private static readonly SpotifyClient Spotify = new(SpotifyConfig);

        public static async Task<SpotifyTrack> Get(string url, bool isId = false)
        {
            var id = isId ? url : url.Split("track/")[1].Split("?si")[0];
            var track = await Spotify.Tracks.Get(id);
            return new SpotifyTrack
            {
                Title = track.Name,
                Author = Methods.ArtistsNameCombine(track.Artists),
                Length = (ulong) track.DurationMs,
                TrackId = track.Id,
                Album = track.Album.Name,
                Explicit = track.Explicit
            };
        }
    }
}