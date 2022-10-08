namespace DiscordBot.Audio.Objects
{
    public struct PlayerInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong CurrentDuration { get; set; }
        public string Current { get; set; }
        public ulong TotalDuration { get; set; }
        public string Total { get; set; }
        public string Loop { get; set; }
        public string ThumbnailUrl { get; set; }
        public bool Paused { get; set; }
        public long Index { get; set; }
    }
}