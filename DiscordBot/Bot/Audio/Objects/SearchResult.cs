namespace DiscordBot.Audio.Objects
{
    public class SearchResult
    {
        public string Title { get; init; }
        public string Author { get; init; }
        public string Length { get; init; }
        public string Url { get; init; }
        public string ThumbnailUrl { get; init; }
        public bool IsSpotify { get; set; }
        public string Id { get; init; }
    }
}