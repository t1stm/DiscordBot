using System.Collections.Generic;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using SpotifyAPI.Web;

namespace Bat_Tosho.Audio.Platforms.Spotify
{
    public static class Playlist
    {
        private static readonly SpotifyClientConfig SpotifyConfig = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator
                ("e90ef778e6e14879a9355acd51a78c2d", "2538249a5e68467e8b3e997c3a1774d2"));

        private static readonly SpotifyClient Spotify = new(SpotifyConfig);

        public static async Task<List<SpotifyTrack>> Get(string url)
        {
            var id = url.Split("playlist/")[1].Split("?si")[0];
            await Debug.Write($"Spotify Playlist Id is: \"{id}\".");
            var tempTracks = await Spotify.Playlists.GetItems(id, new PlaylistGetItemsRequest {Offset = 0});
            var playlistTracks = new List<PlaylistTrack<IPlayableItem>>();
            var offset = 0;
            while (tempTracks.Items != null && tempTracks.Items.Count != 0)
            {
                await Debug.Write(
                    $"Offset is: {tempTracks.Offset}, Count is: {tempTracks.Items.Count}, Total is: {tempTracks.Total}");
                playlistTracks.AddRange(tempTracks.Items);
                offset += 100;
                tempTracks = await Spotify.Playlists.GetItems(id, new PlaylistGetItemsRequest {Offset = offset});
            }

            List<SpotifyTrack> list = new();
            foreach (var item in playlistTracks)
                switch (item.Track)
                {
                    case FullTrack track:
                        list.Add(new SpotifyTrack(track.Name, track.Artists, track.DurationMs));
                        break;
                    case FullEpisode episode:
                        list.Add(new SpotifyTrack(episode.Name, null, 0));
                        break;
                }

            return list;
        }
    }
}