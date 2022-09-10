namespace DiscordBot.Data.Models
{
    public class VideoInformationModel
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public long Length { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}