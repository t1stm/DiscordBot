using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bat_Tosho.Audio;
using Bat_Tosho.Audio.Objects;
using Bat_Tosho.Audio.Platforms.Youtube;
using Bat_Tosho.Enums;
using Bat_Tosho.Methods;
using BatToshoRESTApp.Components;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace BatToshoRESTApp.Controllers
{
    public class BatTosho : Controller
    {
        // GET
        public async Task<string> Search(string searchTerm)
        {
            var client = new YoutubeClient(HttpClient.WithCookies());
            var video = await client.Search.GetVideosAsync(searchTerm).CollectAsync(25);
            var list = new ListSearch(new List<SearchResultTemplate>());
            foreach (var result in video)
                list.Array.Add(new SearchResultTemplate
                {
                    Title = result.Title,
                    AuthorName = result.Author.Title,
                    AuthorUrl = $"https://youtube.com/channel/{result.Author.ChannelId.Value}",
                    Duration = result.Duration?.ToString("hh\\:mm\\:ss"),
                    ThumbnailUrl = result.Thumbnails[0].Url,
                    VideoId = result.Id
                });
            var jsonRes = JsonSerializer.Serialize(list);
            return jsonRes;
        }

        public async Task<string> AddToQueue(string guildId, string clientSecret, string videoId)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            var user = Manager.WebUserInterfaceUsers[clientSecret];
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            var vi = await Video.Get(videoId, VideoSearchTypes.YoutubeVideoId,
                PartOf.YoutubeSearch, user);
            instance.VideoInfos.Add(vi.First());

            return "success";
        }

        public async Task<string> Skip(string guildId, string clientSecret, string times)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            int times2;
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            try
            {
                times2 = string.IsNullOrEmpty(times) ? 1 : Convert.ToInt32(times);
            }
            catch (Exception)
            {
                return "failed";
            }

            instance.CurrentVideoInfo().Stopwatch.Reset();
            instance.CurrentVideoInfo().Paused = false;
            instance.Song += times2 - 1;
            if (instance.Song < -1)
                instance.Song = -1;
            if (instance.Song > instance.VideoInfos.Count - 1)
                instance.Song = instance.VideoInfos.Count - 1;
            await instance.Ffmpeg.Kill();

            return "success";
        }

        public string CurrentSong(string guildId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            var song = instance.CurrentVideoInfo();
            var currentSong = new GenericVideo
            {
                Title = song.Name,
                Author = song.Author,
                CurrentDurationMs = song.Stopwatch.ElapsedMilliseconds,
                CurrentDuration = song.Stopwatch.Elapsed.ToString("hh\\:mm\\:ss"),
                MaxDurationMs = (long) song.Length.TotalMilliseconds,
                MaxDuration = song.Length.ToString("hh\\:mm\\:ss"),
                ThumbnailUrl = song.ThubmnailUrl switch {null => "noImage.png", _ => song.ThubmnailUrl},
                VideoId = song.YoutubeIdOrPathToFile,
                VolumePercent = (int) (instance.TransmitSink.VolumeModifier * 100)
            };
            return JsonSerializer.Serialize(currentSong);
        }

        public string GetList(string guildId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            var list = instance.VideoInfos.Select(vi => new GenericVideo
                {
                    Author = vi.Author,
                    Title = vi.Name,
                    ThumbnailUrl = vi.ThubmnailUrl,
                    MaxDuration = vi.Length.ToString("hh\\:mm\\:ss"),
                    VideoId = vi.PartOf switch
                    {
                        PartOf.LocalFile or PartOf.DiscordAttachment or PartOf.HttpFileStream => "none",
                        _ => vi.YoutubeIdOrPathToFile
                    },
                    Index = instance.VideoInfos.IndexOf(vi)
                })
                .ToList();

            return JsonSerializer.Serialize(list);
        }

        public string ChangeVolume(string guildId, string clientSecret, string percent)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            double percent2;
            try
            {
                percent2 = Convert.ToDouble(percent);
            }
            catch (Exception)
            {
                return "failed";
            }

            instance.TransmitSink.VolumeModifier = percent2 / 100;
            return "success";
        }

        public string Shuffle(string guildId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            try
            {
                Bat_Tosho.Program.Rng = new Random();
                var tempLVi = instance.VideoInfos.OrderBy(_ => Bat_Tosho.Program.Rng.Next()).ToList();
                var tempVi = instance.CurrentVideoInfo();
                tempLVi.Remove(tempVi);
                tempLVi.Insert(0, tempVi);
                instance.VideoInfos = tempLVi;
                instance.Song = 0;
                return "success";
            }
            catch (Exception)
            {
                return "failed";
            }
        }

        public async Task<string> PlayPause(string guildId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            if (instance.VideoInfos.Count < 1)
                return "failed";

            switch (instance.CurrentVideoInfo().Paused)
            {
                case false:
                    instance.CurrentVideoInfo().Paused = true;
                    instance.CurrentVideoInfo().Stopwatch.Stop();
                    instance.Song -= 1;
                    await instance.Ffmpeg.Kill();
                    break;
                case true:
                    instance.CurrentVideoInfo().Paused = false;
                    break;
            }

            return "success";
        }

        public async Task<string> LeaveFromChannel(string guildId, string clientSecret)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            try
            {
                await instance.Statusbar.Stop(instance, "Stopped from the web interface.");
                var vnext = Bat_Tosho.Program.Discord.GetVoiceNext();
                var vnc = vnext.GetConnection(guild);
                vnc.Disconnect();
                instance.VideoInfos.Clear();
                Manager.Instances.Remove(guild);
                return "success";
            }
            catch (Exception)
            {
                return "failed";
            }
        }

        public string MoveSong(string guildId, string clientSecret, int from, int to)
        {
            if (string.IsNullOrEmpty(clientSecret)) return "notAuthenticated";
            if (!Manager.WebUserInterfaceUsers.ContainsKey(clientSecret)) return "notAuthenticated";
            if (from is 0 || to is 0) return "failed";
            Instance instance;
            DiscordGuild guild;
            if (Manager.Instances.Keys.Count < 1)
                return "failed";
            try
            {
                guild = Manager.Instances.Keys.FirstOrDefault(key => key.Id == Convert.ToUInt64(guildId));
            }
            catch (Exception)
            {
                return "failed";
            }

            if (guild == null)
                return "failed";
            try
            {
                instance = Manager.Instances[guild];
            }
            catch (Exception)
            {
                return "failed";
            }

            try
            {
                var chosenSong = instance.VideoInfos.ElementAt(from);
                instance.VideoInfos.Remove(chosenSong);
                instance.VideoInfos.Insert(to, chosenSong);
                return "success";
            }
            catch (Exception)
            {
                return "failed";
            }
        }
    }
}