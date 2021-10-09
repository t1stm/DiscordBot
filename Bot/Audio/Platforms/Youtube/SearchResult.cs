using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Enums;
using DSharpPlus.Entities;

namespace Bat_Tosho.Audio.Platforms.Youtube
{
    public class SearchResult
    {
        public SearchResult(DiscordUser user)
        {
            User = user;
        }

        private DiscordUser User { get; }

        public async Task<List<VideoInformation>> Get(string path, VideoSearchTypes type, PartOf partOf)
        {
            if (string.IsNullOrEmpty(path)) return new List<VideoInformation>();
            try
            {
                return type switch
                {
                    VideoSearchTypes.SearchTerm or VideoSearchTypes.YoutubeVideoId => await Video.Get(path,
                        type, partOf, User),
                    VideoSearchTypes.YoutubePlaylist => await Playlist.Get(path, User),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
            catch (Exception e)
            {
                await Debug.Write($"Failed to download video. \"{e}\"");
                return null;
            }
        }
    }
}