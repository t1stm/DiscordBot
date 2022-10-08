#nullable enable
using System.Text.Json.Serialization;

namespace DiscordBot.Audio.Objects
{
    public static class LyricsApiStuff
    {
        public record HappiApiResponseMusic
        {
            [JsonInclude, JsonPropertyName("album")]
            public string? Album { get; set; }
            [JsonInclude, JsonPropertyName("api_album")]
            public string? ApiAlbum { get; set; }
            [JsonInclude, JsonPropertyName("api_albumsm")]
            public string? ApiAlbumSm { get; set; }
            [JsonInclude, JsonPropertyName("api_artist")]
            public string? ApiArtist { get; set; }
            [JsonInclude, JsonPropertyName("api_lyrics")]
            public string? ApiLyrics { get; set; }
            [JsonInclude, JsonPropertyName("api_track")]
            public string? ApiTrack { get; set; }
            [JsonInclude, JsonPropertyName("api_tracks")]
            public string? ApiTracks { get; set; }
            [JsonInclude, JsonPropertyName("artist")]
            public string? Artist { get; set; }
            [JsonInclude, JsonPropertyName("bpm")]
            public int BPM { get; set; }
            [JsonInclude, JsonPropertyName("cover")]
            public string? Cover { get; set; }
            [JsonInclude, JsonPropertyName("haslyrics")]
            public bool HasLyrics { get; set; }
            [JsonInclude, JsonPropertyName("id_album")]
            public int IdAlbum { get; set; }
            [JsonInclude, JsonPropertyName("id_artist")]
            public int IdArtist { get; set; }
            [JsonInclude, JsonPropertyName("id_track")]
            public int IdTrack { get; set; }
            [JsonInclude, JsonPropertyName("lang")]
            public string? Lang { get; set; }
            [JsonInclude, JsonPropertyName("track")]
            public string? Track { get; set; }
        }

        public class HappiApiMusicResponse
        {
            [JsonInclude, JsonPropertyName("length")]
            public int Length { get; set; }
            [JsonInclude, JsonPropertyName("result")]
            public HappiApiResponseMusic[]? Result { get; set; }
            [JsonInclude, JsonPropertyName("success")]
            public bool Success { get; set; }
        }

        public record HappiApiLyricsResponse
        {
            [JsonInclude, JsonPropertyName("length")]
            public int Length { get; set; }
            [JsonInclude, JsonPropertyName("result")]
            public HappiApiResponseLyrics? Result { get; set; }
            [JsonInclude, JsonPropertyName("success")]
            public bool Success { get; set; }
        }

        public class HappiApiResponseLyrics
        {
            [JsonInclude, JsonPropertyName("album")]
            public string? Album { get; set; }
            [JsonInclude, JsonPropertyName("api_album")]
            public string? ApiAlbum { get; set; }
            [JsonInclude, JsonPropertyName("api_albums")]
            public string? ApiAlbums { get; set; }
            [JsonInclude, JsonPropertyName("api_artist")]
            public string? ApiArtist { get; set; }
            [JsonInclude, JsonPropertyName("api_lyrics")]
            public string? ApiLyrics { get; set; }
            [JsonInclude, JsonPropertyName("api_track")]
            public string? ApiTrack { get; set; }
            [JsonInclude, JsonPropertyName("api_tracks")]
            public string? ApiTracks { get; set; }
            [JsonInclude, JsonPropertyName("artist")]
            public string? Artist { get; set; }
            [JsonInclude, JsonPropertyName("copyright_label")]
            public string? CopyrightLabel { get; set; }
            [JsonInclude, JsonPropertyName("copyright_notice")]
            public string? CopyrightNotice { get; set; }
            [JsonInclude, JsonPropertyName("copyright_text")]
            public string? CopyrightText { get; set; }
            [JsonInclude, JsonPropertyName("id_album")]
            private int IdAlbum { get; set; }
            [JsonInclude, JsonPropertyName("id_artist")]
            private int IdArtist { get; set; }
            [JsonInclude, JsonPropertyName("id_track")]
            private int IdTrack { get; set; }
            [JsonInclude, JsonPropertyName("lang")]
            public string? Lang { get; set; }
            [JsonInclude, JsonPropertyName("lyrics")]
            public string? Lyrics { get; set; }
            [JsonInclude, JsonPropertyName("track")]
            public string? Track { get; set; }
        }
    }
}