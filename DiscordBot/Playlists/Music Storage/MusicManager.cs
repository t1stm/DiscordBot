#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DiscordBot.Language;
using DiscordBot.Playlists.Music_Storage.Objects;

namespace DiscordBot.Playlists.Music_Storage
{
    public static class MusicManager
    {
        public const string WorkingDirectory = $"{Bot.WorkingDirectory}/Music Database";
        private static List<MusicInfo> Items = new();

        public static void LoadItems()
        {
            var artists = Directory.GetDirectories(WorkingDirectory);
            lock (Items)
            {
                Items = new List<MusicInfo>();
                foreach (var artist in artists)
                {
                    Items.AddRange(GetSongs(artist));   
                }
            }
        }

        private static IEnumerable<MusicInfo> GetSongs(string artist)
        {
            var dir = $"{WorkingDirectory}/{artist}";
            var file = $"{dir}/Info.json";
            var songs = Directory.GetFiles(dir);
            var bg = File.Exists($"{dir}/BG");
            if (File.Exists(file))
            {
                var items = JsonSerializer.Deserialize<List<MusicInfo>>(File.OpenRead(file))!;
                return items.Count == songs.Length + 1 ? items : UpdateData(items, songs, bg);
            }
            
            var list = songs.Select(r => ParseFile(r, bg)).ToList();
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

            return new MusicInfo
            {
                OriginalTitle = title,
                OriginalAuthor = author,
                RelativeLocation = string.Join('/', split[^2..]),
                RomanizedTitle = isBulgarian ? Romanize.FromBulgarian(title) : title,
                RomanizedAuthor = romanizedAuthor
            };
        }

        private static List<MusicInfo> UpdateData(IEnumerable<MusicInfo> existing, IEnumerable<string> files, bool isBulgarian = true)
        {
            var list = new List<MusicInfo>();
            list.AddRange(existing);
            var newFiles = files.Where(location => list.All(r => r.RelativeLocation != string.Join('/', location.Split('/')[^2..])));
            list.AddRange(from newFile in newFiles
            select newFile.Split('/')
            into split
            let filename = split[^1]
            let romanizedAuthor = split[^2]
            let filenameSplit = filename.Split('-')
            let author = filenameSplit[0]
            let title = string.Join('.', string.Join('-', filenameSplit[1..]).Split('.')[..^1])
            select new MusicInfo
            {
                OriginalTitle = title,
                OriginalAuthor = author,
                RelativeLocation = string.Join('/', split[^2..]),
                RomanizedTitle = isBulgarian ? Romanize.FromBulgarian(title) : title,
                RomanizedAuthor = romanizedAuthor
            });

            return list;
        }

        public static MusicInfo? SearchByTerm(string term)
        {
            return Items.AsParallel().OrderByDescending(r => ScoreTerm(r, term)).FirstOrDefault();
        }

        private static int ScoreTerm(MusicInfo info, string term)
        {
            var split = term.Split(' ');
            //Continued in the testing app.
            
            return 0;
        }

        private static bool IsEmpty() => Items.Count < 1;
    }
}