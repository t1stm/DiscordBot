#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Methods;
using Result;
using Streams;

namespace DiscordBot.Audio.Objects;

public class YoutubeOverride : PlayableItem
{
    private const string FileLocation = $"{Bot.WorkingDirectory}/dll/YoutubeOverrides.json";
    public static List<YoutubeOverride> Overrides { get; set; } = new();
    public string[]? YoutubeIds { get; init; }
    public string[] Titles { get; init; } = Array.Empty<string>();
    public string[] Authors { get; init; } = Array.Empty<string>();

    public string[] OriginalTitles { get; init; } = Array.Empty<string>();
    public string[] OriginalAuthors { get; init; } = Array.Empty<string>();

    public static YoutubeOverride? FromId(string id)
    {
        return Overrides.FirstOrDefault(r => r.YoutubeIds != null && r.YoutubeIds.Contains(id));
    }

    public static void UpdateOverrides()
    {
        try
        {
            FileStream file;
            if (!File.Exists(FileLocation))
            {
                file = File.Open(FileLocation, FileMode.OpenOrCreate);
                JsonSerializer.Serialize(file, new List<YoutubeOverride>
                {
                    new()
                    {
                        YoutubeIds = new[] { "0123" },
                        Location = "",
                        Length = 420,
                        Titles = new[] { "Test", "Tester" },
                        Authors = new[] { "Chara", "Character" },
                        OriginalTitles = new[] { "Тестър" },
                        OriginalAuthors = new[] { "Герой" }
                    }
                });
                file.Position = 0;
            }
            else
            {
                file = File.OpenRead(FileLocation);
            }

            var json = JsonSerializer.Deserialize<List<YoutubeOverride>>(file);
            if (json == null) throw new DataException("Deserialized Overrides are null.");

            lock (Overrides)
            {
                Overrides = json;
            }

            file.Close();

            Debug.Write("Updated Youtube Overrides.");
        }
        catch (Exception e)
        {
            Debug.Write($"Updating Youtube Overrides failed: \"{e}\"");
        }
    }

    public override string GetName(bool settingsShowOriginalInfo = false)
    {
        return settingsShowOriginalInfo
            ? $"{OriginalTitles[0]} - {OriginalAuthors[0]}"
            : $"{Titles[0]} - {Authors[0]}";
    }

    public override string GetTitle()
    {
        return Titles[0];
    }

    public override string GetAuthor()
    {
        return Authors[0];
    }

    public override async Task<Result<StreamSpreader, Error>> GetAudioData(params Stream[] outputs)
    {
        try
        {
            var fileLocation = GetLocation();
            var stream_spreader = new StreamSpreader(outputs);
            await using var file = File.Open(fileLocation, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            await file.CopyToAsync(stream_spreader);
            stream_spreader.FinishWriting();
            return Result<StreamSpreader, Error>.Success(stream_spreader);
        }
        catch (Exception e)
        {
            await Debug.WriteAsync($"YoutubeOverride GetAudioData method failed: \"{e}\"");
            return Result<StreamSpreader, Error>.Error(new UnknownError());
        }
    }

    public override string? GetId()
    {
        return YoutubeIds?[0];
    }

    public override string? GetThumbnailUrl()
    {
        return null;
    }

    public override string GetAddUrl()
    {
        return $"yt-ov://{YoutubeIds?[0]}";
    }
}