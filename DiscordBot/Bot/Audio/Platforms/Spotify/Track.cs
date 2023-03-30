#nullable enable
using System;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Audio.Objects;
using SpotifyAPI.Web;
using Result;

namespace DiscordBot.Audio.Platforms.Spotify;

public static class Track
{
    private static readonly SpotifyClientConfig SpotifyConfig = SpotifyClientConfig
        .CreateDefault()
        .WithAuthenticator(new ClientCredentialsAuthenticator
            ("e90ef778e6e14879a9355acd51a78c2d", "2538249a5e68467e8b3e997c3a1774d2"));

    private static readonly SpotifyClient Spotify = new(SpotifyConfig);

    public static async Task<Result<PlayableItem, Error>> Get(string url, bool isId = false)
    {
        try
        {
            var id = isId ? url : url.Split("track/")[1].Split("?si")[0];
            var track = await Spotify.Tracks.Get(id);
            return Result<PlayableItem, Error>.Success(new SpotifyTrack
            {
                Title = track.Name,
                Author = Methods.ArtistsNameCombine(track.Artists),
                Length = (ulong)track.DurationMs,
                TrackId = track.Id,
                Album = track.Album.Name,
                Explicit = track.Explicit
            });
        }
        catch (Exception)
        {
            return Result<PlayableItem, Error>.Error(new SpotifyTrackError());
        }
    }
}