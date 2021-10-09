using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bat_Tosho.Audio.Objects;
using DSharpPlus.Entities;

namespace Bat_Tosho.Audio.Platforms.Local
{
    public class Folders
    {
        public List<VideoInformation> GetFolder(string path, DiscordUser user, bool generateTitles)
        {
            var info = new FileInfo();
            string[] files;
            try
            {
                files = Directory.GetFiles(path.Trim());
            }
            catch (Exception e)
            {
                Debug.Write($"Couldn't access directory. {e}", false).RunSynchronously();
                return null;
            }

            return files.Select(file => info.GetInfo(file, user, generateTitles)).ToList();
        }
    }
}