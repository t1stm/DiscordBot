using System.Collections.Generic;
using System.IO;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class PlaylistFileReader
    {
        public static IEnumerable<string> ReadM3UFile(string location)
        {
            var list = new List<string>();

            if (!File.Exists(location)) return list;

            var fs = File.Open(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var textReader = new StreamReader(fs);

            while (!textReader.EndOfStream)
            {
                var line = textReader.ReadLine();
                if (line == null || line.StartsWith('#')) continue;
                list.Add(line);
            }
            
            return list;
        }
    }
}