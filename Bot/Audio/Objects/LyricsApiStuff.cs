namespace BatToshoRESTApp.Audio.Objects
{
    public static class LyricsApiStuff
    {
        public record HappiApiResponseMusic
        {
            public string album;
            public string api_album;
            public string api_albumsm;
            public string api_artist;
            public string api_lyrics;
            public string api_track;
            public string api_tracks;
            public string artist;
            public int bpm;
            public string cover;
            public bool haslyrics;
            public int id_album;
            public int id_artist;
            public int id_track;
            public string lang;
            public string track;
        }

        /*private record HappiApiResponseLyrics (string artist; int id_artist; string track; int id_track; int id_album;
            string album; string lyrics; string api_artist; string api_albums;
            string api_album; string api_tracks; string api_track; string api_lyrics; string lang;
            string copyright_label; string copyright_notice; string copyright_text); */

        public record HappiApiMusicResponse
        {
            public int length;
            public HappiApiResponseMusic[] result;
            public bool success;
        }

        public record HappiApiLyricsResponse
        {
            public int length;
            public HappiApiResponseLyrics result;
            public bool success;
        }

        public record HappiApiResponseLyrics
        {
            public string album;
            public string api_album;
            public string api_albums;
            public string api_artist;
            public string api_lyrics;
            public string api_track;
            public string api_tracks;
            public string artist;
            public string copyright_label;
            public string copyright_notice;
            public string copyright_text;
            private int id_album;
            private int id_artist;
            private int id_track;
            public string lang;
            public string lyrics;
            public string track;
        }
    }
}