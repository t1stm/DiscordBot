using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Timers;
using DiscordBot.Data.Models;
using DiscordBot.Methods;

namespace DiscordBot.Data
{
    public class Manager <T> where T : class
    {
        private List<T> Data { get; set; } = new();
        private readonly string FileLocation;
        private FileStream FileStream { get; set; }
        private readonly Timer SaveTimer = new(); 

        public Manager()
        {
            SaveTimer.Elapsed += ElapsedEvent;
            SaveTimer.Interval = 300000; // ms | 5 Minutes
            SaveTimer.Start();
            var type = typeof(T);
            var name = type.Name;
            FileLocation = name.Length > 5 && name[^5..] is "Model" or "model" ? 
                $"{Bot.WorkingDirectory}/Databases/{name[..^5]}.json" : 
                $"{Bot.WorkingDirectory}/Databases/{name}.json";
        }

        private void ElapsedEvent(object sender, ElapsedEventArgs e)
        {
            SaveToFile();
        }

        public void ReadDatabase()
        {
            if (!File.Exists(FileLocation))
                SaveToFile(Enumerable.Empty<T>().ToList());
            ReadFile();
        }

        public T Read(T searchData)
        {
            lock (Data)
            {
                return searchData switch
                {
                    FuckYoutubeModel fm => Data.AsParallel()
                        .FirstOrDefault(r =>
                            string.Equals(((FuckYoutubeModel) (object) r).SearchTerm, fm.SearchTerm,
                                StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(((FuckYoutubeModel) (object) r).VideoId, fm.VideoId,
                                StringComparison.InvariantCultureIgnoreCase)),
                    GuildsModel gm => Data.AsParallel().FirstOrDefault(r => ((GuildsModel) (object) r).Id == gm.Id),
                    UsersModel um => Data.AsParallel().FirstOrDefault(r => ((UsersModel) (object) r).Id == um.Id),
                    VideoInformationModel vm => Data.AsParallel()
                        .FirstOrDefault(r => ((VideoInformationModel) (object) r).VideoId == vm.VideoId),
                    _ => null
                };
            }
        }
        
        public void Add(T addModel)
        {
            lock (Data)
            {
                if (Data.Count == 0) ReadDatabase();
                Data.Add(addModel);
            }
        }

        private void ReadFile()
        {
            lock (FileStream ?? new object())
            {
                try
                {
                    FileStream = File.Open(FileLocation, FileMode.Open);
                    Data = JsonSerializer.Deserialize<List<T>>(FileStream);
                    FileStream.Close();
                }
                catch (Exception e)
                {
                    Debug.Write($"Failed to update database file \"{nameof(T)}.json\": \"{e}\"");
                }
            }
        }

        public void SaveToFile()
        {
            lock (Data)
            {
                var copy = Data.ToList();
                SaveToFile(copy);
            }
        }
        
        public void SaveToFile(List<T> data)
        {
            lock (FileStream ?? new object())
            {
                try
                {
                    FileStream = File.Open(FileLocation, FileMode.OpenOrCreate);
                    JsonSerializer.Serialize(FileStream, data);
                    FileStream.Close();
                }
                catch (Exception e)
                {
                    Debug.Write($"Failed to update database file \"{nameof(T)}\": \"{e}\"");
                }
            }
        }
    }
}