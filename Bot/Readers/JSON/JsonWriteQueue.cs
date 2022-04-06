using System;
using System.Collections.Generic;

namespace BatToshoRESTApp.Readers
{
    public static class JsonWriteQueue
    {
        private static readonly SearchJsonReader JsonReader = new();
        private static bool _updateRunning;
        private static List<Tuple<string, string>> AddQueue { get; set; } = new();

        public static void Add(string term, string videoId)
        {
            lock (AddQueue)
            {
                AddQueue.Add(new Tuple<string, string>(term, videoId));
            }
        }

        public static void Update()
        {
            if (_updateRunning) return;
            _updateRunning = true;
            lock (AddQueue)
            {
                foreach (var (item1, item2) in AddQueue) JsonReader.AddVideo(item1, item2).Wait();
                AddQueue = new List<Tuple<string, string>>();
            }

            _updateRunning = false;
        }
    }
}