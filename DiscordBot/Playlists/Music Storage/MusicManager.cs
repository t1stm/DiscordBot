#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DiscordBot.Language;
using DiscordBot.Methods;
using DiscordBot.Playlists.Music_Storage.Objects;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class MusicManager
    {
        public const string WorkingDirectory = $"{Bot.WorkingDirectory}/Music Database";
        public static List<MusicInfo> Items = new();

        public static void LoadItems()
        {
            var artists = Directory.GetDirectories(WorkingDirectory);
            lock (Items)
            {
                Items = new List<MusicInfo>();
                foreach (var artist in artists)
                {
                    Items.AddRange(GetSongs(artist.Split('/').Last()));   
                }
            }
        }

        private static IEnumerable<MusicInfo> GetSongs(string artist)
        {
            var dir = $"{WorkingDirectory}/{artist}";
            Debug.Write($"Reading database directory is: {dir}");
            var file = $"{dir}/Info.json";
            var songs = Directory.GetFiles(dir);
            var bg = !File.Exists($"{dir}/EN");
            if (File.Exists(file))
            {
                using var read = File.OpenRead(file); 
                var items = JsonSerializer.Deserialize<List<MusicInfo>>(read)!;
                if (items.Count == songs.Length - 1 - (bg ? 0 : 1)) return items;
                var data = UpdateData(items, songs, bg);
                using var rfs = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                JsonSerializer.Serialize(rfs, data);
                rfs.Close();
                return data;
            }
            
            var list = songs.Select(r => ParseFile(r, bg)).ToList();
            using var fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            JsonSerializer.Serialize(fs, list);
            fs.Close();

            return list;
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
            entry.RelativeLocation ??= string.Join('/', split[^2..]);
            entry.UpdateRandomId();
            Debug.Write($"Generated entry: \"{entry.OriginalTitle} - {entry.OriginalAuthor}\" - \'{entry.RomanizedTitle} - {entry.RomanizedAuthor}\'");
            return entry;
        }

        private static List<MusicInfo> UpdateData(IEnumerable<MusicInfo> existing, IEnumerable<string> files, bool isBulgarian = true)
        {
            var list = new List<MusicInfo>();
            list.AddRange(existing);
            var newFiles = files.Where(location => list.All(r => r.RelativeLocation != string.Join('/', location.Split('/')[^2..])));
            foreach (var newFile in newFiles)
            {
                var split = newFile.Split('/');
                var filename = split[^1];
                var romanizedAuthor = split[^2];

                var filenameSplit = filename.Split('-');
                var author = filenameSplit[0];
                var title = string.Join('.', 
                    string.Join('-', filenameSplit[1..]).Split('.')[..^1]); // This line makes me feel infuriated.
                var entry = MediaInfo.GetInformation(newFile).GetAwaiter().GetResult();
                entry.OriginalTitle ??= title.Trim();
                entry.OriginalAuthor ??= author.Trim();
                entry.RomanizedTitle ??= isBulgarian ? Romanize.FromBulgarian(title).Trim() : title.Trim();
                entry.RomanizedAuthor ??= romanizedAuthor.Trim();
                entry.RelativeLocation ??= string.Join('/', split[^2..]);
            }
            foreach (var item in list)
            {
                item.UpdateRandomId(); // Updates item.Id if is null or boolean is parsed to force it.
            }
            return list;
        }

        public static MusicInfo? SearchOneByTerm(string term)
        {
            return SearchByTerm(term).FirstOrDefault();
        }

        public static IEnumerable<MusicInfo> SearchByTerm(string term)
        {
            return Items.AsParallel().OrderByDescending(r => ScoreTerm(r, term));
        }

        public static MusicInfo? SearchById(string id)
        {
            return Items.AsParallel().FirstOrDefault(r => r.Id == id);
        }

        private static int ScoreTerm(MusicInfo info, string term)
        {
            var split = term.Split(' ');
            //Continued in the testing app.
            
            return 0;
        }
    }
}