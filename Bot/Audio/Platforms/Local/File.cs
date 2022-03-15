using System.Linq;
using BatToshoRESTApp.Audio.Objects;

namespace BatToshoRESTApp.Audio.Platforms.Local
{
    public static class File
    {
        public static SystemFile GetInfo(string path)
        {
            if (path.Split("/").Last().Length < 4) return null;
            var filename = path.Split("/").Last();
            var file = new SystemFile
            {
                Title = filename,
                Author = path[..^filename.Length],
                Location = path,
                Length = 0,
                IsDiscordAttachment = false
            };
            return file;
        }
    }
}