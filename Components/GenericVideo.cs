namespace BatToshoRESTApp.Components
{
    public class GenericVideo
    {
        public string Title { get; init; }
        public string Author { get; init; }
        public string CurrentDuration { get; init; }
        public long CurrentDurationMs { get; init; }
        public string MaxDuration { get; init; }
        public long MaxDurationMs { get; init; }
        public string ThumbnailUrl { get; init; }
        public string VideoId { get; init; }

        public int Index { get; init; }
        public int VolumePercent { get; init; }
    }
}