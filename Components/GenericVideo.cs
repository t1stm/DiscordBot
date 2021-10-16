namespace BatToshoRESTApp.Components
{
    public class GenericVideo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string CurrentDuration { get; set; }
        public long CurrentDurationMs { get; set; }
        public string MaxDuration { get; set; }
        public long MaxDurationMs { get; set; }
        public string ThumbnailUrl { get; set; }
        public string VideoId { get; set; }

        public int Index { get; set; }
        public int VolumePercent { get; set; }
    }
}