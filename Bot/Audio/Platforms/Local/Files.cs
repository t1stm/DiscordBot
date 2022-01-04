using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatToshoRESTApp.Audio.Objects;

namespace BatToshoRESTApp.Audio.Platforms.Local
{
    public static class Files
    {
        public static List<IPlayableItem> Get(string path)
        {
            var list = new List<IPlayableItem>();
            if (!Directory.Exists(path))
                return new List<IPlayableItem>
                {
                    File.GetInfo(path)
                };

            var files = Directory.GetFileSystemEntries(path);
            list.AddRange(files.Select(File.GetInfo));
            return list;
        }
    }
}