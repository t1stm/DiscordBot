using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Audio.Objects;
using SpotifyAPI.Web;

namespace DiscordBot.Audio.Platforms.Spotify
{
    public static class Playlist
    {
        private static readonly SpotifyClientConfig SpotifyConfig = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator
                ("e90ef778e6e14879a9355acd51a78c2d", "2538249a5e68467e8b3e997c3a1774d2"));

        private static readonly SpotifyClient Spotify = new(SpotifyConfig);

        public static async Task<Result<List<PlayableItem>, Error>> Get(string id)
        {
            try
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

                List<PlayableItem> list = new();
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

                return Result<List<PlayableItem>, Error>.Success(list);
            }
            catch (Exception)
            {
                return Result<List<PlayableItem>, Error>.Error(new SpotifyPlaylistError());
            }
        }

        public static async Task<Result<List<PlayableItem>, Error>> GetAlbum(string albumId)
        {
            try
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

                return Result<List<PlayableItem>, Error>.Success(playlistTracks.Select(track => new SpotifyTrack
                    {
                        Title = track.Name,
                        Author = Methods.ArtistsNameCombine(track.Artists),
                        Length = (ulong) track.DurationMs,
                        TrackId = track.Id,
                        Album = null,
                        Explicit = track.Explicit
                    }).Cast<PlayableItem>()
                    .ToList());
            }
            catch (Exception)
            {
                return Result<List<PlayableItem>, Error>.Error(new SpotifyPlaylistError());
            }
        }
    }
}