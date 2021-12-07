using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BatToshoRESTApp.Readers
{
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
            if (search.Any(si => si.SearchTerm == searchTerm || si.VideoId == id)) return;
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