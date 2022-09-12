using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Data.Models
{
    public class FuckYoutubeModel : IModel<FuckYoutubeModel>
    {
        public string SearchTerm { get; set; }
        public string VideoId { get; set; }
        public FuckYoutubeModel Read(IEnumerable<FuckYoutubeModel> source)
        {
            return source.AsParallel()
                .FirstOrDefault(r => string.Equals(r.SearchTerm, SearchTerm,
                                         StringComparison.InvariantCultureIgnoreCase) ||
                                     string.Equals(r.VideoId, VideoId,
                                         StringComparison.InvariantCultureIgnoreCase));
        }
    }
}