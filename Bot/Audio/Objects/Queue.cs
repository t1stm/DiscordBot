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
        public List<IPlayableItem> Items = new();
        public int Current { get; set; }
        public long Count
        {
            get
            {
                lock (Items)
                {
                    return Items.Count;
                }
            }
        }

        public int RandomSeed { get; private set; }

        public void AddToQueue(IPlayableItem info)
        {
            lock (Items) Items.Add(info);
        }

        public void AddToQueue(IEnumerable<IPlayableItem> infos)
        {
            lock (Items) Items.AddRange(infos);
        }

        public void AddToQueueNext(IEnumerable<IPlayableItem> infos)
        {
            lock (Items) Items.InsertRange(Current + 1, infos);
        }

        public void AddToQueueNext(IPlayableItem info)
        {
            lock (Items) Items.Insert(Current + 1, info);
        }

        public IPlayableItem RemoveFromQueue(int index)
        {
            lock (Items)
            {
                var item = Items.ElementAt(index);
                Items.Remove(item);
                return item;
            }
        }

        public IPlayableItem RemoveFromQueue(IPlayableItem item)
        {
            lock (Items)
            {
                Items.Remove(item);
                return item;
            }
        }

        public IPlayableItem RemoveFromQueue(string name)
        {
            lock (Items)
            {
                var item = Items.First(vi => LevenshteinDistance.Compute(vi.GetName(), name) < 3);
                Items.Remove(item);
                return item;
            }
        }

        public IPlayableItem GetWithString(string name)
        {
            lock (Items)
            {
                return Items.First(vi => LevenshteinDistance.Compute(vi.GetName(), name) < 3);
            }
        }

        public async Task DownloadAll()
        {
            if (_downloadTasks > 0) return;
            _downloadTasks = 1;
            try
            {
                IEnumerable<IPlayableItem> dll;
                lock (Items) dll = Items.Where(it => string.IsNullOrEmpty(it.GetLocation()));
                var playableItems = dll.ToArray();
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < playableItems.Length; i++)
                {
                    var pl = playableItems[i];
                    if (pl is SpotifyTrack tr)
                    {
                        int index;
                        lock (Items) index = Items.IndexOf(pl);
                        var newI = await new Video().Search(tr);
                        lock (Items)
                        {
                            Items[index] = newI;
                        }
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
            lock (Items)
            {
                var queue = Items.OrderBy(_ => new Random().Next()).ToList();
                var current = GetCurrent();
                queue.Remove(current);
                queue.Insert(0, current);
                Items = queue;
                Current = 0;
            }
        }

        public void Clear()
        {
            var current = GetCurrent();
            lock (Items)
            {
                Current = 0;
                Items = new List<IPlayableItem> {current};
            }
        }

        public void ShuffleWithSeed(int seed)
        {
            lock (Items)
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
        }

        public IPlayableItem GetCurrent()
        {
            lock (Items)
            {
                return Items[Current];
            }
        }

        public IPlayableItem GetNext()
        {
            lock (Items)
            {
                return Items[Current + 1];
            }
        }

        public bool Move(int result, int thing2, out IPlayableItem item)
        {
            item = null;
            try
            {
                lock (Items)
                {
                    var iOne = Items[result];
                    Items.Remove(iOne);
                    Items.Insert(thing2, iOne);
                    item = iOne;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.Write($"Move int command error: {e}");
                return false;
            }
        }

        public override string ToString()
        {
            string result = "";
            lock (Items)
            {
                if (Items.Count < 1) return "The queue is empty";
                for (var index = 0; index < Items.Count; index++)
                {
                    var item = Items[index];
                    if (index == Current)
                    {
                        result += new string('-', item.GetName().Length + 8) + '\n';
                        result += $"({Items.IndexOf(item) + 1}) - \"{item.GetName()}\"\n";
                        result += new string('-', item.GetName().Length + 8) + '\n';
                    }
                    else result += $"({Items.IndexOf(item) + 1}) - \"{item.GetName()}\"\n";
                }
            }

            return result;
        }
        public bool Move(string result, string thing2, out IPlayableItem item1, out IPlayableItem item2)
        {
            item1 = null;
            item2 = null;
            try
            {
                lock (Items)
                {
                    var one = Items.FirstOrDefault(vi =>
                        LevenshteinDistance.Compute(vi.GetName(), result.Trim()) < vi.GetName().Length * 0.2);
                    var two = Items.FirstOrDefault(vi =>
                        LevenshteinDistance.Compute(vi.GetName(), thing2.Trim()) < vi.GetName().Length * 0.2);
                    if (one == null || two == null)
                    {
                        one = Items.OrderBy(vi =>
                            LevenshteinDistance.Compute(vi.GetTitle(), result.Trim())).FirstOrDefault();
                        two = Items.OrderBy(vi =>
                            LevenshteinDistance.Compute(vi.GetTitle(), thing2.Trim())).FirstOrDefault();
                        if (one == null || two == null) return false;
                    }
                    var iOne = Items.IndexOf(one);
                    var iTwo = Items.IndexOf(two);
                    Items[iOne] = two;
                    Items[iTwo] = one;
                    item2 = Items[iOne];
                    item1 = Items[iTwo];
                }
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