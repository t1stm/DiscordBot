using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Methods;
using Result;
using Streams;
using TagLib;
using File = System.IO.File;
using TagFile = TagLib.File;

namespace DiscordBot.Audio.Objects;

public class SystemFile : PlayableItem
{
    public bool IsDiscordAttachment { get; init; }
    public ulong Guild { get; init; }

    public override string GetThumbnailUrl()
    {
        return "";
    }

    public override string GetLocation()
    {
        return IsDiscordAttachment
            ? $"{Bot.WorkingDirectory}/dll/Discord Attachments/{Guild}/{Location}"
            : base.GetLocation();
    }

    public override string GetAddUrl()
    {
        return IsDiscordAttachment switch
        {
            true => $"dis-att://{Guild}-{Location}",
            false => $"file://{Location}"
        };
    }

    public override ulong GetLength()
    {
        return Length == default ? 0 : Length;
    }

    public override Task ProcessInfo()
    {
        if (Processed) return Task.CompletedTask;
        Processed = true;
        try
        {
            var info = TagFile.Create(GetLocation());
            Length = (ulong)info.Properties.Duration.TotalMilliseconds + 0;
            var tag = info.GetTag(TagTypes.AllTags);
            if (tag == null) return Task.CompletedTask;
            Title = string.IsNullOrEmpty(tag.Title) ? Title : tag.Title;
            Author = string.IsNullOrEmpty(tag.JoinedPerformers) ? Author : tag.JoinedPerformers;
        }
        catch
        {
            // Ignored
        }

        return Task.CompletedTask;
    }

    public override async Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        try
        {
            var fileLocation = GetLocation();
            var stream_spreader = new StreamSpreader(outputs);
            await using var file = File.Open(fileLocation, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            await file.CopyToAsync(stream_spreader).ContinueWith(_ => { stream_spreader.FinishWriting(); })
                .ConfigureAwait(false);
            return Result<StreamSpreader, Error>.Success(stream_spreader);
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"MusicObject GetAudioData method failed: \"{e}\"");
            return Result<StreamSpreader, Error>.Error(new UnknownError());
        }
    }

    public override string GetId()
    {
        return "";
    }
}