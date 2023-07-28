#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DiscordBot.Audio.Objects;
using DiscordBot.Language;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage.Objects;
using DiscordBot.Tools;
using Newtonsoft.Json;

namespace DiscordBot.Playlists.Music_Storage;

public static class MusicManager
{
    public const string WorkingDirectory = $"{Bot.WorkingDirectory}/Music Database";
    public const string AlbumCoverAddress = $"{Bot.SiteDomain}/Album_Covers";
    private static List<MusicInfo> Items = new();
    private static List<Album> Albums = new();

    public static void Load()
    {
        var startingDirectories = Directory.GetDirectories(WorkingDirectory);
        LoadItems(startingDirectories);
        ExtractImages.Run();
        LoadItems(startingDirectories, true);
        LoadAlbums(startingDirectories);
    }

    private static void LoadItems(IEnumerable<string> startingDirectories, bool isVerbose = false)
    {
        lock (Items)
        {
            Items.Clear();
            Items = new List<MusicInfo>();
            foreach (var directory in startingDirectories) // This is bad, but I can't think of a better solution.
            {
                var artists = Directory.GetDirectories(directory);
                foreach (var artist in artists)
                    Items.AddRange(GetSongs(directory, artist.Split('/').Last(), isVerbose));
            }

            foreach (var info in Items) info.CoverUrl = info.CoverUrl?.Replace("$[DOMAIN]", AlbumCoverAddress);
        }
    }

    private static void LoadAlbums(IEnumerable<string> startingDirectories)
    {
        lock (Albums)
        {
            Albums.Clear();
            Albums = new List<Album>();
            foreach (var directory in startingDirectories)
            {
                var artists = Directory.GetDirectories(directory);
                foreach (var artist in artists) Albums.AddRange(ReadAlbums(artist));
            }
        }
    }

    private static IEnumerable<Album> ReadAlbums(string dir)
    {
        var songs = Directory.GetFiles(dir);
        if (!songs.Any(IsM3UPlaylist))
            return Enumerable.Empty<Album>();

        var list = new List<Album>();

        foreach (var playlist in songs.Where(IsM3UPlaylist))
            try
            {
                var album = Album.FromM3U(playlist);
                album.Artist = album.Songs.First().OriginalAuthor;
                Debug.Write($"Loaded album: \"{album.AlbumName}\" by artist: \'{album.Artist}\'");
                list.Add(album);
            }
            catch (Exception e)
            {
                Debug.Write($"Adding album to list failed with error: \"{e}\"");
            }

        return list;
    }

    private static bool IsIgnored(string source)
    {
        return source.EndsWith("EN") ||
               IsM3UPlaylist(source) ||
               source.EndsWith(".txt") ||
               source.EndsWith(".pls") ||
               source.EndsWith(".json");
    }

    private static bool IsM3UPlaylist(string source)
    {
        return source.EndsWith(".m3u") || source.EndsWith(".m3u8");
    }

    private static IEnumerable<MusicInfo> GetSongs(string directory, string artist, bool isVerbose = false)
    {
        var dir = $"{directory}/{artist}";
        if (isVerbose) Debug.Write($"Reading database directory: {dir}");
        var jsonFile = $"{dir}/Info.json";

        var songs = Directory.GetFiles(dir);
        var ignored = songs.Count(IsIgnored);

        var serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };
        using var file_stream = File.Open(jsonFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

        if (File.Exists(jsonFile))
        {
            using var reader = new StreamReader(file_stream, Encoding.UTF8, true, 1024, true);

            var json = reader.ReadToEnd();
            var items = JsonConvert.DeserializeObject<List<MusicInfo>>(json) ?? Enumerable.Empty<MusicInfo>().ToList();

            if (items.Count == songs.Length - ignored) return items;
            var data = UpdateData(items, songs).ToList();

            // Reset the position after reading to end.
            file_stream.Position = 0;

            using var writer = new StreamWriter(file_stream, Encoding.UTF8);
            serializer.Serialize(writer, data);

            return data;
        }

        var list = songs.Where(r => !IsIgnored(r)).Select(ParseFile).ToList();

        using var no_file_writer = new StreamWriter(file_stream);
        serializer.Serialize(no_file_writer, list);
        file_stream.Close();

        return list;
    }

    private static IEnumerable<MusicInfo> UpdateData(IEnumerable<MusicInfo> existing, IEnumerable<string> files)
    {
        var existingList = existing.ToList();
        foreach (var mi in existingList) yield return mi;

        var newFiles = files.Where(location =>
            !IsIgnored(location) &&
            existingList.All(r => r.RelativeLocation != string.Join('/', location.Split('/')[^3..])));

        foreach (var newFile in newFiles) yield return ParseFile(newFile);
    }

    private static MusicInfo ParseFile(string location)
    {
        var split = location.Split('/');
        var filename = split[^1];
        var romanizedAuthor = split[^2];

        var filenameSplit = filename.Split(" - ");
        var author = filenameSplit[0];
        var title = string.Join('.',
            string.Join('-', filenameSplit[1..]).Split('.')[..^1]); // This line makes me feel infuriated.
        var entry = MediaInfo.GetInformation(location).GetAwaiter().GetResult();
        entry.OriginalTitle ??= title.Trim();
        entry.OriginalAuthor ??= author.Trim();
        entry.RomanizedTitle ??= Romanize.FromBulgarian(title).Trim();
        entry.RomanizedAuthor ??= romanizedAuthor.Trim();
        entry.RelativeLocation ??= string.Join('/', split[^3..]);
        entry.UpdateRandomId();
        Debug.Write(
            $"Generated entry: \"{entry.OriginalTitle} - {entry.OriginalAuthor}\" - \'{entry.RomanizedTitle} - {entry.RomanizedAuthor}\'");
        return entry;
    }

    public static MusicInfo? SearchOneByTerm(string term)
    {
        return Items.AsParallel()
            .FirstOrDefault(r => ScoreSingleTerm(term, r));
    }

    private static bool ScoreSingleTerm(string term, MusicInfo r)
    {
        return LevenshteinDistance.ComputeLean(r.RomanizedTitle, term) < 2 ||
               LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} - {r.RomanizedAuthor}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} {r.RomanizedAuthor}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} {r.RomanizedTitle}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} - {r.RomanizedTitle}", term) < 3 ||
               LevenshteinDistance.ComputeLean(r.OriginalTitle, term) < 2 ||
               LevenshteinDistance.ComputeLean($"{r.OriginalTitle} - {r.OriginalAuthor}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.OriginalTitle} {r.OriginalAuthor}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} {r.OriginalTitle}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} - {r.OriginalTitle}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.OriginalTitle} - {r.RomanizedAuthor}", term) < 3 ||
               LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} - {r.OriginalAuthor}", term) < 3;
    }

    public static MusicInfo? SearchFromSpotify(SpotifyTrack track)
    {
        //var searchTerm = $"{track.Author} - {track.Title}";
        var author_weight = Math.Ceiling((float)track.Author.Length / 3);
        var matching_authors = Items.AsParallel().Where(r =>
            LevenshteinDistance.ComputeLean(r.OriginalAuthor, track.Author) < author_weight ||
            LevenshteinDistance.ComputeLean(r.RomanizedAuthor, track.Author) < author_weight).ToArray();

        if (matching_authors.Length < 1) return null;

        var title_weight = Math.Ceiling((float)track.Title.Length / 5);

        var matching_titles = matching_authors.AsParallel().Where(r =>
            LevenshteinDistance.ComputeLean(r.OriginalTitle, track.Title) < title_weight ||
            LevenshteinDistance.ComputeLean(r.RomanizedTitle, track.Title) < title_weight).ToArray();

        return matching_titles.Length switch
        {
            < 1 => null,
            1 => matching_titles.First(),
            > 1 => matching_titles.MinBy(r =>
            {
                var romanized_score = LevenshteinDistance.ComputeLean(r.RomanizedTitle, track.Title);
                var original_score = LevenshteinDistance.ComputeLean(r.OriginalTitle, track.Title);
                return original_score > romanized_score ? original_score : romanized_score;
            })
        };
    }

    public static IEnumerable<MusicInfo> OrderByTerm(string term)
    {
        if (string.IsNullOrEmpty(term)) return GetAll();

        var ordered = from r in Items.AsParallel()
            orderby
                Min(
                    // Romanized data pass.
                    LevenshteinDistance.ComputeLean(r.RomanizedTitle, term),
                    LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} - {r.RomanizedAuthor}", term),
                    LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} {r.RomanizedAuthor}", term),
                    LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} {r.RomanizedTitle}", term),
                    LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} - {r.RomanizedTitle}", term),
                    // Original data pass.
                    LevenshteinDistance.ComputeLean(r.OriginalTitle, term),
                    LevenshteinDistance.ComputeLean($"{r.OriginalTitle} - {r.OriginalAuthor}", term),
                    LevenshteinDistance.ComputeLean($"{r.OriginalTitle} {r.OriginalAuthor}", term),
                    LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} {r.OriginalTitle}", term),
                    LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} - {r.OriginalTitle}", term),
                    // Added step to help me find songs. I hope this doesn't break anything.
                    LevenshteinDistance.ComputeLean(r.RomanizedAuthor, term),
                    LevenshteinDistance.ComputeLean(r.OriginalAuthor, term))
            select r;

        return ordered;
    }

    private static int Min(params int[] values)
    {
        return values.Min();
    }

    public static IEnumerable<MusicInfo> SearchByPattern(string search)
    {
        try
        {
            var pattern = search;
            if (pattern.Length == 0) return Enumerable.Empty<MusicInfo>();
            pattern = pattern.Replace("*", ".*");
            return Items.AsParallel().Where(r => Regex.IsMatch(r.RelativeLocation ?? "", $"^{pattern}"));
        }
        catch (Exception)
        {
            Debug.Write("Pattern search failed.");
            return Enumerable.Empty<MusicInfo>();
        }
    }

    public static MusicInfo? SearchById(string id)
    {
        return Items.AsParallel().FirstOrDefault(r => r.Id == id) ??
               // Second pass for regenerated infos.
               Items.AsParallel().FirstOrDefault(r => (r.Id ?? "  ")[..^2] == id[..^2]);
    }

    public static IEnumerable<MusicInfo> GetAll()
    {
        var copy = Items.ToArray();
        return copy;
    }

    public static IEnumerable<Album> GetAllAlbums()
    {
        return Albums.ToArray();
    }

    public static IEnumerable<Album> SearchAlbums(string term)
    {
        if (string.IsNullOrEmpty(term)) return GetAllAlbums();
        var lower = term.ToLower();
        var sanitized = Regex.Escape(lower);

        var found = Albums.AsParallel().Where(r => Regex.IsMatch(r.AlbumName.ToLower(), $".*{sanitized}.*"));
        var ordered = found.OrderBy(r => LevenshteinDistance.ComputeStrict(term, r.AlbumName.ToLower()));

        return ordered;
    }

    public static Album? SearchAlbumById(string id)
    {
        return Albums.AsParallel().FirstOrDefault(r => r.AccessString == id);
    }

    private static int ScoreTerm(MusicInfo info, string term)
    {
        var split = term.Split(' ');
        //Continued in the testing app.

        return 0;
    }
}