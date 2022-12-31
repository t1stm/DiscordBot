using System.Collections.Generic;
using System.IO;
using DiscordBot.Abstract;

namespace DiscordBot.Audio.Platforms.Local
{
    public static class Files
    {
        public static List<PlayableItem> Get(string path)
        {
            var list = new List<PlayableItem>();
            if (!Directory.Exists(path))
                return new List<PlayableItem>
                {
                    File.GetInfo(path)
                };

            var files = Directory.GetFileSystemEntries(path);
            foreach (var file in files) list.AddRange(Get(file));
            return list;
        }
    }
}