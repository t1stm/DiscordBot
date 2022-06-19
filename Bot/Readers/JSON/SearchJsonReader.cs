using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Methods;
using DiscordBot.Readers.Objects;

namespace DiscordBot.Readers.JSON
{
    [Obsolete(" This class is obseleted, by the new Database implementation. Reminder to myself, to not use it.")]
    public class SearchJsonReader : IBaseJson
    {
        public async Task<string> Read()
        {
            return await File.ReadAllTextAsync($"{Bot.WorkingDirectory}/FuckYoutube.json");
        }

        public async Task Write(string json)
        {
            await File.WriteAllTextAsync($"{Bot.WorkingDirectory}/FuckYoutube.json", json);
        }

        public async Task<PreviousSearchResult> GetVideo(string term)
        {
            var json = await Read();
            var t = term.ToLower();
            var search = JsonSerializer.Deserialize<List<PreviousSearchResult>>(json);
            var obj = search?.FirstOrDefault(si => si.SearchTerm == t);
            if (obj == null)
                await Debug.WriteAsync($"Couldn't find video in the JSON file with term: {t}", false,
                    Debug.DebugColor.Warning);
            return obj;
        }

        public async Task<List<PreviousSearchResult>> GetAllResults()
        {
            return JsonSerializer.Deserialize<List<PreviousSearchResult>>(await Read());
        }

        public async Task AddVideo(string searchTerm, string id)
        {
            var search = await GetAllResults();
            searchTerm = searchTerm.ToLower();
            if (search == null) return;
            if (search.Any(si => si.SearchTerm == searchTerm && si.VideoId == id)) return;
            var f = new PreviousSearchResult
            {
                SearchTerm = searchTerm,
                VideoId = id
            };
            search.Add(f);
            var e = JsonSerializer.Serialize(search);
            await Write(e);
        }
    }
}