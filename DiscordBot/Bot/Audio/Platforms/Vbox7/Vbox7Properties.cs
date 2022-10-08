namespace DiscordBot.Audio.Platforms.Vbox7
{
    public record Vbox7Properties
    {
        public string Src { get; set; }
        public string Title { get; set; }
        public string Uploader { get; set; }
        public string Vid { get; set; }
        public ulong Duration { get; set; }
    }
}