namespace CustomPlaylistFormat.Objects
{
    public struct Playlist
    {
        public PlaylistInfo? Info;
        public bool FailedToParse;
        public Entry[]? PlaylistItems;
    }
}