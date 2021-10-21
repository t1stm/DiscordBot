using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using SpotifyAPI.Web;

namespace Bat_Tosho.Audio.Platforms.Spotify
{
    public static class Track
    {
        private static readonly SpotifyClientConfig SpotifyConfig = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator
                ("e90ef778e6e14879a9355acd51a78c2d", "2538249a5e68467e8b3e997c3a1774d2"));

        private static readonly SpotifyClient Spotify = new(SpotifyConfig);

        public static async Task<SpotifyTrack> Get(string url)
        {
            var id = url.Split("track/")[1].Split("?si")[0];
            await Debug.Write($"Spotify Playlist Id is: \"{id}\".");
            var track = await Spotify.Tracks.Get(id);
            return new SpotifyTrack(track.Name, track.Artists, track.DurationMs);
        }
    }
}