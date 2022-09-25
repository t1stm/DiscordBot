using System;
using System.Collections.Generic;
using System.Linq;
using DiscordBot.Audio.Objects;

namespace DiscordBot.Data.Models
{
    public class VideoInformationModel : Model<VideoInformationModel>
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }
        public string ThumbnailUrl { get; set; }

        public override VideoInformationModel SearchFrom(IEnumerable<VideoInformationModel> source)
        {
            return source.AsParallel().FirstOrDefault(r => VideoId == r.VideoId);
        }
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