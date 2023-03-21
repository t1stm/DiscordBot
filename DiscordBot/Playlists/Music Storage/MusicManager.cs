#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiscordBot.Audio.Objects;
using DiscordBot.Language;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage.Objects;
using DiscordBot.Tools;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class MusicManager
    {
        public static readonly string WorkingDirectory = $"{Bot.WorkingDirectory}/Music Database";
        public static readonly string AlbumCoverAddress = $"{Bot.SiteDomain}/Album_Covers";
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
            if (File.Exists(jsonFile))
            {
                using var read = File.OpenRead(jsonFile);
                var items = JsonSerializer.Deserialize<List<MusicInfo>>(read) ?? Enumerable.Empty<MusicInfo>().ToList();
                if (items.Count == songs.Length - ignored) return items;
                var data = UpdateData(items, songs).ToList();
                using var rfs = File.Open(jsonFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                JsonSerializer.Serialize(rfs, data);
                rfs.Close();
                return data;
            }

            var list = songs.Where(r => !IsIgnored(r)).Select(ParseFile).ToList();
            using var fs = File.Open(jsonFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            JsonSerializer.Serialize(fs, list);
            fs.Close();

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

            var filenameSplit = filename.Split('-');
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
                   LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} - {r.RomanizedAuthor}", term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} {r.RomanizedAuthor}", term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} {r.RomanizedTitle}", term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} - {r.RomanizedTitle}", term) < 2 ||
                   LevenshteinDistance.ComputeLean(r.OriginalTitle, term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.OriginalTitle} - {r.OriginalAuthor}", term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.OriginalTitle} {r.OriginalAuthor}", term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} {r.OriginalTitle}", term) < 2 ||
                   LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} - {r.OriginalTitle}", term) < 2;
        }

        public static MusicInfo? SearchFromSpotify(SpotifyTrack track)
        {
            var searchTerm = $"{track.Author} - {track.Title}";
            return SearchOneByTerm(searchTerm);
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
            return Items.AsParallel().FirstOrDefault(r => r.Id == id);
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
}