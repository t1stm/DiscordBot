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
        public int Current { get; set; }
        public long Count => Items.Count;

        public int RandomSeed { get; set; }

        public void AddToQueue(IPlayableItem info)
        {
            Items.Add(info);
        }

        public void AddToQueue(IEnumerable<IPlayableItem> infos)
        {
            Items.AddRange(infos);
        }

        public void AddToQueueNext(IEnumerable<IPlayableItem> infos)
        {
            Items.InsertRange(Current + 1, infos);
        }

        public void AddToQueueNext(IPlayableItem info)
        {
            Items.Insert(Current + 1, info);
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

        public async Task DownloadAll()
        {
            if (_downloadTasks > 0) return;
            _downloadTasks = 1;
            try
            {
                var dll = Items.Where(it => string.IsNullOrEmpty(it.GetLocation()));
                var playableItems = dll.ToArray();
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

                    if (pl.GetIfErrored()) continue;

                    await Debug.WriteAsync($"Downloading {pl.GetName()}");
                    if (string.IsNullOrEmpty(pl.GetLocation())) await pl.Download();
                }
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Error in updating songs: \"{e}\"");
            }

            _downloadTasks = 0;
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

        public void ShuffleWithSeed(int seed)
        {
            if (seed == -555) seed = new Random().Next(int.MaxValue);
            var queue = Items.OrderBy(_ => new Random(seed).Next()).ToList();
            var current = GetCurrent();
            queue.Remove(current);
            queue.Insert(0, current);
            Items = queue;
            Current = 0;
            RandomSeed = seed;
        }

        public IPlayableItem GetCurrent()
        {
            return Items[Current];
        }

        public IPlayableItem GetNext()
        {
            return Items[Current + 1];
        }

        public bool Move(int result, int thing2)
        {
            try
            {
                var one = Items[result];
                var two = Items[thing2];
                if (one == null || two == null) return false;
                Items[result] = two;
                Items[thing2] = one;
                return true;
            }
            catch (Exception e)
            {
                Debug.Write($"Move int command error: {e}");
                return false;
            }
        }

        public bool Move(string result, string thing2)
        {
            try
            {
                var one = Items.FirstOrDefault(vi =>
                    LevenshteinDistance.Compute(vi.GetName(), result.Trim()) < vi.GetName().Length * 0.2);
                var two = Items.FirstOrDefault(vi =>
                    LevenshteinDistance.Compute(vi.GetName(), thing2.Trim()) < vi.GetName().Length * 0.2);
                if (one == null || two == null) return false;
                var iOne = Items.IndexOf(one);
                var iTwo = Items.IndexOf(two);
                Items[iOne] = two;
                Items[iTwo] = one;
                return true;
            }
            catch (Exception e)
            {
                Debug.Write($"Move string command error: {e}");
                return false;
            }
        }
    }
}