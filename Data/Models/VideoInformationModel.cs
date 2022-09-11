using DiscordBot.Audio.Objects;

namespace DiscordBot.Data.Models
{
    public class VideoInformationModel
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }
        public string ThumbnailUrl { get; set; }

        public YoutubeVideoInformation Convert()
        {
            return new()
            {
                Title = Title,
                Author = Author,
                Length = Length,
                YoutubeId = VideoId,
                ThumbnailUrl = ThumbnailUrl
            };
        }
    }
}