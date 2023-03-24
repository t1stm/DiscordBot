using System;
using System.Linq;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Audio.Objects;

namespace DiscordBot.Audio.Platforms.Local;

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

    public static Result<SystemFile, Error> GetInfo(string path, ulong guild) // This is for the Discord Attachments
    {
        try
        {
            if (path.Split("/").Last().Length < 4) return null;
            var filename = path.Split("/").Last();
            var file = new SystemFile
            {
                Title = filename,
                Author = path[..^filename.Length],
                Location = path,
                Length = 0,
                IsDiscordAttachment = true,
                Guild = guild
            };
            return Result<SystemFile, Error>.Success(file);
        }
        catch (Exception)
        {
            return Result<SystemFile, Error>.Error(new UnknownError());
        }
    }
}