using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Data.Models;
using DiscordBot.Methods;

namespace DiscordBot.Data.Managers
{
    public static class FuckYoutube
    {
        private const string FileLocation = $"{Bot.WorkingDirectory}/Database/FuckYoutube.json";
        private static List<FuckYoutubeModel> Data { get; set; } = new();
        private static FileStream FileStream { get; set; }

        public static void ReadDatabase()
        {
            if (!File.Exists(FileLocation))
                UpdateFile(Enumerable.Empty<FuckYoutubeModel>().ToList());
            ReadFile();
        }

        public static FuckYoutubeModel Read(string searchTerm)
        {
            lock (Data) return Data.AsParallel().FirstOrDefault(r => string.Equals(r.SearchTerm, searchTerm, StringComparison.CurrentCultureIgnoreCase));
        }
        
        public static void Add(FuckYoutubeModel addModel)
        {
            lock (Data)
            {
                if (Data.Count == 0) ReadDatabase();
                Data.Add(addModel);
                var copy = Data.ToList(); // To avoid threading issues.
                new Task(() =>
                {
                    UpdateFile(copy);
                }).Start();
            }
        }

        private static void ReadFile()
        {
            lock (FileStream)
            {
                try
                {
                    FileStream = File.Open(FileLocation, FileMode.Open);
                    Data = JsonSerializer.Deserialize<List<FuckYoutubeModel>>(FileStream);
                    FileStream.Close();
                }
                catch (Exception e)
                {
                    Debug.Write($"Failed to update database file \"FuckYoutube.json\": \"{e}\"");
                }
            }
        }

        private static void UpdateFile(List<FuckYoutubeModel> data)
        {
            lock (FileStream)
            {
                try
                {
                    FileStream = File.Open(FileLocation, FileMode.OpenOrCreate);
                    JsonSerializer.Serialize(FileStream, data);
                    FileStream.Close();
                }
                catch (Exception e)
                {
                    Debug.Write($"Failed to update database file \"FuckYoutube.json\": \"{e}\"");
                }
            }
        }
    }
}