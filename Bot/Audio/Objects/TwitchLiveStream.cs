using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;
using Debug = BatToshoRESTApp.Methods.Debug;

namespace BatToshoRESTApp.Audio.Objects
{
    public class TwitchLiveStream : PlayableItem
    {
        private new string Title { get; set; } = "";

        private string Description { get; set; } = "";
        private new string Location { get; set; }

        private bool Running { get; set; }

        public string Url { get; set; }

        private new bool Errored { get; set; }

        public new string GetName()
        {
            return Title == "" ? Url : Title;
        }

        public new ulong GetLength()
        {
            return 0;
        }

        public new string GetLocation()
        {
            if (!string.IsNullOrEmpty(Location)) return Location;
            var task = new Task(async () => await Download());
            task.Start();
            task.Wait();
            return Location;
        }

        public override async Task Download()
        {
            try
            {
                if (Running)
                {
                    while (Running) await Task.Delay(166);
                    return;
                }

                Running = true;
                var dlp = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Arguments =
                        $"-e --get-description -g -f bestaudio {Url}",
                    FileName = "yt-dlp"
                };
                var pr = Process.Start(dlp);
                if (pr == null)
                {
                    Errored = true;
                    return;
                }

                await pr.WaitForExitAsync();
                await Debug.WriteAsync($"Url: \"{Url}\"");
                var text = await pr.StandardOutput.ReadToEndAsync();
                var spl = text.Split("\n");
                Description = spl[0];
                Location = spl[1];
                Title = spl[2];

                Running = false;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Twitch Content finding URL failed: \"{e.Message}\"");
                Errored = true;
            }
        }

        public override string GetId()
        {
            return "";
        }

        public override string GetTypeOf()
        {
            return "Twitch Content";
        }

        public override string GetThumbnailUrl()
        {
            return null;
        }
    }
}