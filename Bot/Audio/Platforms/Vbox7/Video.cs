using System;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using BatToshoRESTApp.Tools;

namespace BatToshoRESTApp.Audio.Platforms.Vbox7
{
    public class Video
    {
        [Obsolete("This method of getting the Vbox7 video information is slow and very brain damaged. Please use the other methods.")]
        public async Task<IPlayableItem> Search(string searchTerm)
        {
            try
            {
                var response1 =
                    await HttpClient.GetSourceCodeAfterLoadingPage(
                        $"https://vbox7.com/search?vbox_q={searchTerm.Replace(" ", "+").Replace("+", "%2B")}");
                var yes1 = response1.Split(@"class=""info-cont""")[1].Split("\n");
                foreach (var line in yes1)
                {
                    if (!line.Contains(@"a href")) continue;
                    return await GetVideoByUri(line.Split(@"href=""")[1].Split(@"""")[0]);
                }

                throw new Exception();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
        
        [Obsolete("This method of getting the Vbox7 video information is slow and very brain damaged. Please use the other methods.")]
        public async Task<IPlayableItem> GetVideoByUri(string reqUri)
        {
            var response = await HttpClient.GetSourceCodeAfterLoadingPage($"https://vbox7.com{reqUri}");
            Vbox7Video item = new();
            try
            {
                item.Length = (ulong) StringToTimeSpan
                    .Generate(response.Split(@"class=""vbox-timer-total"">")[1].Split("<")[0]).TotalMilliseconds;
                item.Location =
                    response.Split(@"<video id=""html5player"" playsinline="""" tabindex=""0"" data-src=""")[1]
                        .Split("\"")[0];
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Failed adding Location or Setting Length: {e}");
            }

            try
            {
                item.Title = response.Split(@"class=""title-row""")[1].Split("<h1>")[1]
                    .Split("</h1>")[0].Trim();
                item.Author = response.Split(@"class=""channel-name""")[1].Split("<span>")[1]
                    .Split("</span>")[0].Trim();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Failed adding Name or Author: {e}");
            }

            return item;
        }
    }
}