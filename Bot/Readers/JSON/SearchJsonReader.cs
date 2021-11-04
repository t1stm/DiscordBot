using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BatToshoRESTApp.Readers
{
    public class SearchJsonReader : IBaseJson
    {
        public string Read()
        {
            return File.ReadAllText($"{Bot.WorkingDirectory}/FuckYoutube.json");
        }

        public void Write(string json)
        {
            File.WriteAllText($"{Bot.WorkingDirectory}/FuckYoutube.json", json);
        }

        public PreviousSearchResult GetVideo(string term)
        {
            var json = Read();
            var t = term.ToLower();
            var search = JsonSerializer.Deserialize<List<PreviousSearchResult>>(json);
            return search?.FirstOrDefault(si => si.SearchTerm == t);
        }

        public List<PreviousSearchResult> GetAllResults()
        {
            return JsonSerializer.Deserialize<List<PreviousSearchResult>>(Read());
        }

        public void AddVideo(string searchTerm, string id)
        {
            var search = GetAllResults();
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
            Write(e);
        }
    }
}