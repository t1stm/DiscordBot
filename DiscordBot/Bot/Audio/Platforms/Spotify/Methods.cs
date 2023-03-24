using System.Collections.Generic;
using SpotifyAPI.Web;

namespace DiscordBot.Audio.Platforms.Spotify;

public static class Methods
{
    public static string ArtistsNameCombine(List<SimpleArtist> ar)
    {
        var artist = "";
        for (var index = 0; index < ar.Count; index++)
        {
            var sa = ar[index];
            artist += $"{index switch { 0 => "", _ => ", " }}{sa.Name}";
        }

        return artist;
    }
}