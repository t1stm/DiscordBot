using System.Collections.Generic;
using SpotifyAPI.Web;

namespace Bat_Tosho.Audio.Objects
{
    public class SpotifyTrack
    {
        public readonly string ArtistsCombined = "";
        public readonly string SearchTerm;
        public readonly string TrackName;
        public readonly int LengthMs;

        public SpotifyTrack(string trackName, IEnumerable<SimpleArtist> artists, int lengthMs)
        {
            TrackName = trackName;
            LengthMs = lengthMs;
            foreach (var artist in artists) ArtistsCombined += $"{artist.Name}, ";

            ArtistsCombined = ArtistsCombined.Remove(ArtistsCombined.Length - 2);
            SearchTerm = $"{TrackName}{ArtistsCombined switch {"" => "", _ => $" - {ArtistsCombined}"}}";
        }
    }
}