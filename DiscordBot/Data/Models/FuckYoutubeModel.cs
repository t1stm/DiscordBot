#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class FuckYoutubeModel : Model<FuckYoutubeModel>
    {
        public string SearchTerm { get; set; } = null!;
        public string VideoId { get; set; } = null!;

        public override FuckYoutubeModel? SearchFrom(IEnumerable<FuckYoutubeModel> source)
        {
            return source.AsParallel()
                .FirstOrDefault(r => string.Equals(r.SearchTerm, SearchTerm,
                                         StringComparison.InvariantCultureIgnoreCase) ||
                                     string.Equals(r.VideoId, VideoId,
                                         StringComparison.InvariantCultureIgnoreCase));
        }
    }
}