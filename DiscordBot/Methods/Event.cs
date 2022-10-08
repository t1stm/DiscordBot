using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordBot.Methods
{
    public static class Event
    {
        private static bool _updateRunning;
        private static List<Tuple<string, string>> AddQueue { get; set; } = new();

        public static void Add(string member, string message)
        {
            lock (AddQueue)
            {
                AddQueue.Add(new Tuple<string, string>(member, message));
            }
        }

        public static void Update()
        {
            if (_updateRunning) return;
            _updateRunning = true;
            lock (AddQueue)
            {
                foreach (var (item1, item2) in AddQueue) Answers.AddAnswer(item1, item2);
                AddQueue = new List<Tuple<string, string>>();
            }

            _updateRunning = false;
        }
    }

    public static class Answers
    {
        private static bool _writing;

        public static void AddAnswer(string user, string message)
        {
            while (_writing) Task.Delay(66).Wait();
            _writing = true;
            var file = File.Open($"{Bot.WorkingDirectory}/Votes.json", FileMode.OpenOrCreate);
            try
            {
                var answers = file.Length == 0 ? new List<Answer>() : JsonSerializer.Deserialize<List<Answer>>(file);
                answers?.Add(new Answer
                {
                    User = user,
                    Message = message
                });
                file.Write(JsonSerializer.SerializeToUtf8Bytes(answers));
                file.Close();
                _writing = false;
            }
            catch (Exception e)
            {
                Debug.Write($"Setting Anwser failed: {e}");
                file.Close();
            }
        }
    }

    public class Answer
    {
        public string User { get; set; }
        public string Message { get; set; }
    }
}