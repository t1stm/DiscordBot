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
    public class DatabaseManager <T> where T : IModel<T>
    {
        private List<T> Data { get; set; } = new();
        private readonly string FileLocation;
        private readonly string ObjectName;
        private FileStream FileStream { get; set; }
        private readonly Timer SaveTimer = new();
        private bool Modified;

        public DatabaseManager()
        {
            SaveTimer.Elapsed += ElapsedEvent;
            SaveTimer.Interval = 300000; // ms | 5 Minutes
            SaveTimer.Start();
            var type = typeof(T);
            var name = type.Name;
            ObjectName = name.Length > 5 && name[^5..] is "Model" or "model" ? name[..^5] : name;
            FileLocation = $"{Bot.WorkingDirectory}/Databases/{ObjectName}.json";
        }

        private void ElapsedEvent(object sender, ElapsedEventArgs e)
        {
            if (!Modified) return;
            if (Bot.DebugMode) Debug.Write($"Saving \"{ObjectName}\" database.");
            SaveToFile();
        }

        public void ReadDatabase()
        {
            if (!File.Exists(FileLocation))
                SaveToFile(Enumerable.Empty<T>().ToList());
            ReadFile();
        }

        public T Read(T searchData, bool markModified = false)
        {
            if (markModified) Modified = true;
            lock (Data) return searchData.Read(Data); // This makes me go over the rainbow.
        }

        public List<T> ReadCopy()
        {
            lock (Data)
            {
                return Data.ToList();
            }
        }
        
        public void Add(T addModel)
        {
            lock (Data)
            {
                if (Data.Count == 0) ReadDatabase();
                Data.Add(addModel);
                Modified = true;
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
                    Debug.Write($"Failed to update database file \"{ObjectName}.json\": \"{e}\"");
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
        
        private void SaveToFile(List<T> data)
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
                    Debug.Write($"Failed to update database file \"{ObjectName}\": \"{e}\"");
                }
            }
        }
    }
}