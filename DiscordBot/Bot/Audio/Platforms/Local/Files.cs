using System;
using System.Collections.Generic;
using System.IO;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;

namespace DiscordBot.Audio.Platforms.Local
{
    public static class Files
    {
        public static Result<List<PlayableItem>, Error> Get(string path)
        {
            try
            {
                var list = new List<PlayableItem>();
                if (!Directory.Exists(path))
                    return Result<List<PlayableItem>, Error>.Success(new List<PlayableItem>
                    {
                        File.GetInfo(path)
                    });

                var files = Directory.GetFileSystemEntries(path);
                foreach (var file in files)
                {
                    var recursiveCall = Get(file);
                    if (recursiveCall != Status.OK) continue;
                    list.AddRange(recursiveCall.GetOK());
                }

                return Result<List<PlayableItem>, Error>.Success(list);
            }
            catch (Exception)
            {
                return Result<List<PlayableItem>, Error>.Error(new UnknownError());
            }
        }
    }
}