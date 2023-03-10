#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Audio.Platforms.Youtube;
using DiscordBot.Methods;
using DiscordBot.Objects;
using DiscordBot.Tools;

namespace DiscordBot.Audio
{
    public class Queue
    {
        private int _downloadTasks;
        public List<PlayableItem> Items = new();
        public WebSocketManager? Manager { get; set; }
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

        public bool EndOfQueue => Current >= Count;

        public int RandomSeed { get; private set; }

        private void Broadcast()
        {
            var task = new Task(async () =>
            {
                try
                {
                    if (Manager != null)
                        await Manager.BroadcastQueue();
                    else throw new Exception("The WebSocketManager is null.");
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Broadcasting queue failed: \"{e}\"", false, Debug.DebugColor.Error);
                }
            });
            task.Start();
        }

        private void Update(int index)
        {
            var task = new Task(async () =>
            {
                try
                {
                    PlayableItem item;
                    lock (Items)
                    {
                        item = Items[index];
                    }

                    if (Manager != null)
                        await Manager.BroadcastUpdateItem(index, item.ToSearchResult());
                    else throw new Exception("The WebSocketManager is null.");
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"Broadcasting new item failed: \"{e}\"", false, Debug.DebugColor.Error);
                }
            });
            task.Start();
        }

        public void AddToQueue(PlayableItem info)
        {
            lock (Items)
            {
                Items.Add(info);
            }

            Broadcast();
        }

        public void AddToQueue(IEnumerable<PlayableItem> infos)
        {
            lock (Items)
            {
                Items.AddRange(infos);
            }

            Broadcast();
        }

        public void AddToQueueNext(IEnumerable<PlayableItem> infos)
        {
            lock (Items)
            {
                Items.InsertRange(Current + 1, infos);
            }

            Broadcast();
        }

        public void AddToQueueNext(PlayableItem info)
        {
            lock (Items)
            {
                Items.Insert(Current + 1, info);
            }

            Broadcast();
        }

        public PlayableItem RemoveFromQueue(int index)
        {
            lock (Items)
            {
                var item = Items.ElementAt(index);
                Items.Remove(item);
                Broadcast();
                return item;
            }
        }

        public PlayableItem RemoveFromQueue(PlayableItem item)
        {
            lock (Items)
            {
                Items.Remove(item);
                Broadcast();
                return item;
            }
        }

        public PlayableItem RemoveFromQueue(string name)
        {
            lock (Items)
            {
                var item = Items.First(vi => LevenshteinDistance.ComputeStrict(vi.GetName(), name) < 3);
                Items.Remove(item);
                Broadcast();
                return item;
            }
        }

        public PlayableItem GetWithString(string name)
        {
            lock (Items)
            {
                return Items.First(vi => LevenshteinDistance.ComputeLean(vi.GetName(), name) < 3);
            }
        }

        public async Task ProcessAll()
        {
            if (_downloadTasks > 0) return;
            _downloadTasks = 1;
            try
            {
                IEnumerable<PlayableItem> dll;
                lock (Items)
                {
                    dll = Items.Where(it => string.IsNullOrEmpty(it.GetLocation()));
                }

                var playableItems = dll.ToArray();
                // ReSharper disable once ForCanBeConvertedToForeach
                // This is so it doesn't throw when the Items' count is changed.
                for (var i = 0; i < playableItems.Length; i++)
                {
                    var pl = playableItems[i];
                    if (pl is SpotifyTrack tr)
                    {
                        int index;
                        lock (Items)
                        {
                            index = Items.IndexOf(pl);
                        }

                        var newI = await Video.Search(tr);
                        if (newI == null) continue;
                        
                        lock (Items)
                        {
                            Items[index] = newI;
                        }

                        await newI.ProcessInfo();
                        Update(index);
                        continue;
                    }

                    if (pl.GetIfErrored()) continue;
                    if (pl.Processed) continue;
                    await Debug.WriteAsync(
                        $"Updating info of {pl.GetTypeOf(Parser.FromNumber(0))} : \"{pl.GetName()}\"");
                    if (string.IsNullOrEmpty(pl.GetLocation())) await pl.ProcessInfo();
                    Broadcast();
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
                var random = new Random();
                var queue = Items.OrderBy(_ => random.Next()).ToList();
                var current = GetCurrent();
                if (current is not null)
                {
                    queue.Remove(current);
                    queue.Insert(0, current);
                }

                Items = queue;
                Current = 0;
                Broadcast();
            }
        }

        public void Clear()
        {
            var current = GetCurrent();
            lock (Items)
            {
                Current = 0;
                Items = new List<PlayableItem>();
                if (current is not null) Items.Add(current);
            }

            Broadcast();
        }

        public void ShuffleWithSeed(int seed)
        {
            lock (Items)
            {
                if (seed == -555) seed = new Random().Next(int.MaxValue);
                var queue = Items.OrderBy(_ => new Random(seed).Next()).ToList();
                var current = GetCurrent();
                if (current is not null)
                {
                    queue.Remove(current);
                    queue.Insert(0, current);
                }

                Items = queue;
                Current = 0;
                RandomSeed = seed;
            }
        }

        public PlayableItem? GetCurrent()
        {
            lock (Items)
            {
                return Items.Count == 0 ? null : Current >= Items.Count || Current < 0 ? null : Items[Current];
            }
        }

        public PlayableItem? GetNext()
        {
            lock (Items)
            {
                return Current == Items.Count - 1 ? null : Items[Current + 1];
            }
        }

        public bool Move(int result, int thing2, out PlayableItem item)
        {
            item = null!;
            try
            {
                lock (Items)
                {
                    var iOne = Items[result];
                    Items.Remove(iOne);
                    Items.Insert(thing2, iOne);
                    item = iOne;
                }

                Broadcast();
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
            var result = "";
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
                    else
                    {
                        result += $"({Items.IndexOf(item) + 1}) - \"{item.GetName()}\"\n";
                    }
                }
            }

            return result;
        }

        public bool Move(string result, string thing2, out PlayableItem item1, out PlayableItem item2)
        {
            item1 = null!;
            item2 = null!;
            try
            {
                lock (Items)
                {
                    var one = Items.FirstOrDefault(vi =>
                        LevenshteinDistance.ComputeStrict(vi.GetName(), result.Trim()) < vi.GetName().Length * 0.2);
                    var two = Items.FirstOrDefault(vi =>
                        LevenshteinDistance.ComputeStrict(vi.GetName(), thing2.Trim()) < vi.GetName().Length * 0.2);
                    if (one == null || two == null)
                    {
                        one = Items.OrderBy(vi =>
                            LevenshteinDistance.ComputeStrict(vi.GetTitle(), result.Trim())).FirstOrDefault();
                        two = Items.OrderBy(vi =>
                            LevenshteinDistance.ComputeStrict(vi.GetTitle(), thing2.Trim())).FirstOrDefault();
                        if (one == null || two == null) return false;
                    }

                    var iOne = Items.IndexOf(one);
                    var iTwo = Items.IndexOf(two);
                    Items[iOne] = two;
                    Items[iTwo] = one;
                    item2 = Items[iOne];
                    item1 = Items[iTwo];
                }

                Broadcast();
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