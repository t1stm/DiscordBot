namespace DiscordBot.Audio.Objects
{
    public static class LyricsApiStuff
    {
        public record HappiApiResponseMusic
        {
            public string album { get; set; }
            public string api_album { get; set; }
            public string api_albumsm { get; set; }
            public string api_artist { get; set; }
            public string api_lyrics { get; set; }
            public string api_track { get; set; }
            public string api_tracks { get; set; }
            public string artist { get; set; }
            public int bpm { get; set; }
            public string cover { get; set; }
            public bool haslyrics { get; set; }
            public int id_album { get; set; }
            public int id_artist { get; set; }
            public int id_track { get; set; }
            public string lang { get; set; }
            public string track { get; set; }
        }

        /*private record HappiApiResponseLyrics (string artist; int id_artist; string track; int id_track; int id_album;
            string album; string lyrics; string api_artist; string api_albums;
            string api_album; string api_tracks; string api_track; string api_lyrics; string lang;
            string copyright_label; string copyright_notice; string copyright_text); */

        public record HappiApiMusicResponse
        {
            public int length { get; set; }
            public HappiApiResponseMusic[] result { get; set; }
            public bool success { get; set; }
        }

        public record HappiApiLyricsResponse
        {
            public int length { get; set; }
            public HappiApiResponseLyrics result { get; set; }
            public bool success { get; set; }
        }

        public record HappiApiResponseLyrics
        {
            public string album { get; set; }
            public string api_album { get; set; }
            public string api_albums { get; set; }
            public string api_artist { get; set; }
            public string api_lyrics { get; set; }
            public string api_track { get; set; }
            public string api_tracks { get; set; }
            public string artist { get; set; }
            public string copyright_label { get; set; }
            public string copyright_notice { get; set; }
            public string copyright_text { get; set; }
            private int id_album { get; set; }
            private int id_artist { get; set; }
            private int id_track { get; set; }
            public string lang { get; set; }
            public string lyrics { get; set; }
            public string track { get; set; }
        }
    }
}