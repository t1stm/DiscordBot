#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage;
using Result;
using Streams;

namespace DiscordBot.Audio.Objects;

public class MusicObject : PlayableItem
{
    public string? RomanizedTitle { get; init; }
    public string? RomanizedAuthor { get; init; }
    public string? RelativeLocation { get; init; }
    public string? AddId { get; init; }
    public string? CoverUrl { get; init; }

    public override string GetName(bool settingsShowOriginalInfo = false)
    {
        return !settingsShowOriginalInfo && RomanizedTitle != null
            ? $"{RomanizedTitle}{(RomanizedAuthor != null ? $" - {RomanizedAuthor}" : "")}"
            : $"{Title}{(Author != null ? $" - {Author}" : "")}";
    }

    public override string GetLocation()
    {
        return $"{MusicManager.WorkingDirectory}/{RelativeLocation}";
    }

    public override async Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        try
        {
            var fileLocation = GetLocation();
            var stream_spreader = new StreamSpreader(outputs);
            await using var file = File.Open(fileLocation, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            await file.CopyToAsync(stream_spreader).ContinueWith(_ =>
            {
                stream_spreader.FinishWriting();
            }).ConfigureAwait(false);
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
        return AddId ?? "";
    }

    public override string GetThumbnailUrl()
    {
        return CoverUrl ?? "";
    }

    public override string GetAddUrl()
    {
        return $"audio://{GetId()}";
    }
}