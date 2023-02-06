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
        public static List<MusicInfo> Items = new();

        public static void LoadItems()
        {
            lock (Items)
            {
                Items.Clear();
                var startingDirectories = Directory.GetDirectories(WorkingDirectory);
                Items = new List<MusicInfo>();
                foreach (var directory in startingDirectories) // This is bad, but I can't think of a better solution.
                {
                    var artists = Directory.GetDirectories(directory);
                    foreach (var artist in artists) Items.AddRange(GetSongs(directory, artist.Split('/').Last()));
                }

                foreach (var info in Items)
                {
                    info.CoverUrl = info.CoverUrl?.Replace("$[DOMAIN]", AlbumCoverAddress);
                }
            }
        }

        private static IEnumerable<MusicInfo> GetSongs(string directory, string artist)
        {
            var dir = $"{directory}/{artist}";
            Debug.Write($"Reading database directory: {dir}");
            var jsonFile = $"{dir}/Info.json";

            var songs = Directory.GetFiles(dir);
            var bg = !File.Exists($"{dir}/EN");
            if (File.Exists(jsonFile))
            {
                using var read = File.OpenRead(jsonFile);
                var items = JsonSerializer.Deserialize<List<MusicInfo>>(read) ?? Enumerable.Empty<MusicInfo>().ToList();
                if (items.Count == songs.Length - 1 - (bg ? 0 : 1)) return items;
                var data = UpdateData(items, songs, bg).ToList();
                using var rfs = File.Open(jsonFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                JsonSerializer.Serialize(rfs, data);
                rfs.Close();
                return data;
            }

            var list = songs.Where(r => !r.EndsWith("EN")).Select(r => ParseFile(r, bg)).ToList();
            using var fs = File.Open(jsonFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            JsonSerializer.Serialize(fs, list);
            fs.Close();

            return list;
        }

        private static IEnumerable<MusicInfo> UpdateData(IEnumerable<MusicInfo> existing, IEnumerable<string> files,
            bool isBulgarian = true)
        {
            var existingList = existing.ToList();
            foreach (var mi in existingList) yield return mi;

            var newFiles = files.Where(location =>
                !location.EndsWith(".json") && !location.EndsWith("EN") &&
                existingList.All(r => r.RelativeLocation != string.Join('/', location.Split('/')[^3..])));

            foreach (var newFile in newFiles) yield return ParseFile(newFile, isBulgarian);
        }

        private static MusicInfo ParseFile(string location, bool isBulgarian = true)
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
            entry.RomanizedTitle ??= isBulgarian ? Romanize.FromBulgarian(title).Trim() : title.Trim();
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
                .FirstOrDefault(r =>
                    LevenshteinDistance.ComputeLean(r.RomanizedTitle, term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} - {r.RomanizedAuthor}", term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.RomanizedTitle} {r.RomanizedAuthor}", term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} {r.RomanizedTitle}", term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.RomanizedAuthor} - {r.RomanizedTitle}", term) < 2 ||
                    
                    LevenshteinDistance.ComputeLean(r.OriginalTitle, term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.OriginalTitle} - {r.OriginalAuthor}", term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.OriginalTitle} {r.OriginalAuthor}", term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} {r.OriginalTitle}", term) < 2 ||
                    LevenshteinDistance.ComputeLean($"{r.OriginalAuthor} - {r.OriginalTitle}", term) < 2);
        }
        
        public static MusicInfo? SearchFromSpotify(SpotifyTrack track)
        {
            var searchTerm = $"{track.Author} - {track.Title}";
            return SearchOneByTerm(searchTerm);
        }

        public static IEnumerable<MusicInfo> SearchByTerm(string term)
        {
            return Items.AsParallel().OrderByDescending(r => ScoreTerm(r, term));
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
            var copy = Items.ToList();
            return copy;
        }

        private static int ScoreTerm(MusicInfo info, string term)
        {
            var split = term.Split(' ');
            //Continued in the testing app.

            return 0;
        }
    }
}