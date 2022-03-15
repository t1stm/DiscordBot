using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using SpotifyAPI.Web;
using IPlayableItem = SpotifyAPI.Web.IPlayableItem;

namespace BatToshoRESTApp.Audio.Platforms.Spotify
{
    public static class Playlist
    {
        private static readonly SpotifyClientConfig SpotifyConfig = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator
                ("e90ef778e6e14879a9355acd51a78c2d", "2538249a5e68467e8b3e997c3a1774d2"));

        private static readonly SpotifyClient Spotify = new(SpotifyConfig);

        public static async Task<List<SpotifyTrack>> Get(string id)
        {
            var tempTracks = await Spotify.Playlists.GetItems(id, new PlaylistGetItemsRequest {Offset = 0});
            var playlistTracks = new List<PlaylistTrack<IPlayableItem>>();
            var offset = 0;
            while (tempTracks.Items != null && tempTracks.Items.Count != 0)
            {
                playlistTracks.AddRange(tempTracks.Items);
                offset += 100;
                tempTracks = await Spotify.Playlists.GetItems(id, new PlaylistGetItemsRequest {Offset = offset});
            }

            List<SpotifyTrack> list = new();
            foreach (var item in playlistTracks)
                switch (item.Track)
                {
                    case FullTrack track:
                        list.Add(new SpotifyTrack
                        {
                            Title = track.Name,
                            Author = Methods.ArtistsNameCombine(track.Artists),
                            Length = (ulong) track.DurationMs,
                            TrackId = track.Id,
                            Album = track.Album.Name,
                            Explicit = track.Explicit
                        });
                        break;
                }

            return list;
        }

        public static async Task<List<SpotifyTrack>> GetAlbum(string albumId)
        {
            var tempTracks = await Spotify.Albums.GetTracks(albumId, new AlbumTracksRequest {Offset = 0});
            var playlistTracks = new List<SimpleTrack>();
            var offset = 0;
            while (tempTracks.Items != null && tempTracks.Items.Count != 0)
            {
                playlistTracks.AddRange(tempTracks.Items);
                offset += 100;
                tempTracks = await Spotify.Albums.GetTracks(albumId, new AlbumTracksRequest {Offset = offset});
            }
            return playlistTracks.Select(track => new SpotifyTrack
                {
                    Title = track.Name,
                    Author = Methods.ArtistsNameCombine(track.Artists),
                    Length = (ulong) track.DurationMs,
                    TrackId = track.Id,
                    Album = null,
                    Explicit = track.Explicit
                })
                .ToList();
        }
    }
}