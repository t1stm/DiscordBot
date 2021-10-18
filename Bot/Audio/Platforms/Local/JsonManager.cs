using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bat_Tosho.Audio.Objects;

namespace Bat_Tosho.Audio.Platforms.Local
{
    public static class JsonManager
    {
        private static string JsonFile { get; set; }
        private static readonly string Filename = $"{Program.MainDirectory}FuckYoutube.json";
        public static async Task<string> Read(string search)
        {
            JsonFile ??= await File.ReadAllTextAsync(Filename);
            if (string.IsNullOrEmpty(JsonFile))
            {
                JsonFile = JsonSerializer.Serialize(new List<SearchMatches>
                {
                    new ()
                    {
                        SearchTerm = "ihope no one~ever_searches__this_please_dont come on for a biscuit please. ok thanks",
                        VideoId = "thanks"
                    }
                });
                await File.WriteAllTextAsync(Filename, JsonFile);
            }
            var json = JsonSerializer.Deserialize<List<SearchMatches>>(JsonFile);
            var element = json?.FirstOrDefault(si => si.SearchTerm == search);
            return element?.VideoId;
        }

        public static async Task Write(string search, string videoId)
        {
            JsonFile ??= await File.ReadAllTextAsync(Filename);
            var json = JsonSerializer.Deserialize<List<SearchMatches>>(JsonFile);
            json?.Add(new SearchMatches
            {
                SearchTerm = search,
                VideoId = videoId
            });
            JsonFile = JsonSerializer.Serialize(json);
            await File.WriteAllTextAsync(Filename, JsonFile);
        }
        private class SearchMatches
        {
            public string SearchTerm { get; init; }
            public string VideoId { get; init; }
        }
    }
}