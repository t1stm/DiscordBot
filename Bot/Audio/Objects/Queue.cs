using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Platforms.Youtube;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Tools;

namespace BatToshoRESTApp.Audio.Objects
{
    public class Queue
    {
        private int _downloadTasks;
        public List<IPlayableItem> Items { get; set; } = new();
        public int Current { get; set; } = 0;
        public long Count => Items.LongCount();

        public void AddToQueue(IPlayableItem info)
        {
            Items.Add(info);
        }

        public async Task DownloadAll()
        {
            if (_downloadTasks > 0) return;
            _downloadTasks = 1;
            var task = new Task(async () =>
            {
                try
                {
                    var dll = Items.Where(it => string.IsNullOrEmpty(it.GetLocation()));
                    var playableItems = dll as IPlayableItem[] ?? dll.ToArray();
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < playableItems.Length; i++)
                    {
                        var pl = playableItems[i];
                        if (pl is SpotifyTrack tr)
                        {
                            var index = Items.IndexOf(pl);
                            var newI = await new Video().Search(tr);
                            Items[index] = newI;
                            await newI.Download();
                            continue;
                        }

                        await Debug.WriteAsync($"Downloading {pl.GetName()}");
                        if (string.IsNullOrEmpty(pl.GetLocation())) await pl.Download();
                    }
                }
                catch (Exception)
                {
                    await Debug.WriteAsync("Error in updating songs.");
                }
            });
            task.Start();
            await task;
            _downloadTasks = 0;
        }

        public void AddToQueue(IEnumerable<IPlayableItem> infos)
        {
            Items.AddRange(infos);
        }

        public void RemoveFromQueue(int index)
        {
            Items.RemoveAt(index);
        }

        public void RemoveFromQueue(IPlayableItem item)
        {
            Items.Remove(item);
        }

        public bool RemoveFromQueue(string name)
        {
            return Items.Remove(Items.First(vi => LevenshteinDistance.Compute(vi.GetName(), name) < 3));
        }

        public void Shuffle()
        {
            var queue = Items.OrderBy(_ => new Random().Next()).ToList();
            var current = GetCurrent();
            queue.Remove(current);
            queue.Insert(0, current);
            Items = queue;
            Current = 0;
        }

        public IPlayableItem GetCurrent()
        {
            return Items[Current];
        }

        public IPlayableItem GetNext()
        {
            return Items[Current + 1];
        }
    }
}