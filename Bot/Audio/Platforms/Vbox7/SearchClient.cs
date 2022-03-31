using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;

namespace BatToshoRESTApp.Audio.Platforms.Vbox7
{
    public static class SearchClient
    {
        // I TAKA NEKA SLEDVASHTIQ MUZIKALEN POZDRAV ZVUCHI SPECIALNO, ZA MOQ BRAT SASHO....SKORPIONA,
        // ZA RUSI SHAMPIONA, ZA IHTI DRAKONA, ZA MOITE PRIQTELI ANDI BAR I DOLNA METROPOLIA, ZA GALI,
        // ZA SHUREKA, ZA KALI, ZA CQLATA KOMPANIA, ZA NASHIQ ZAPISVACH, D.. ZA DJ MECHO TESKIQ, HA HA HA ha
        // https://www.vbox7.com/play:a3769b5b6b - Suraikata 2014 mix
        public static async Task<List<Vbox7Object>> GetResultsFromSearch(string term)
        {
            var list = new List<Vbox7Object>();
            var call = await HttpClient.DownloadStream($"https://www.vbox7.com/search?vbox_q={term}");
            var yes = Encoding.UTF8.GetString(call.GetBuffer());
            var yes1 = yes.Split(@"class=""info-cont""")[1].Split("\n");
            foreach (var line in yes1)
            {
                if (!line.Contains(@"a href=""/play")) continue;
                list.Add(await SearchById(line.Split("/play:")[1].Split("\"")[0]));
            }
            return list;
        }

        public static async Task<Vbox7Object> SearchUrl(string url)
        {
            if (url.Length < 7) throw new InvalidCredentialException(nameof(url)); 
            url = url.StartsWith("http://") ? url[7..] :
                url.StartsWith("https://") ? url[8..] : "";
            await Debug.WriteAsync(url);
            if (url == "") throw new InvalidCredentialException(nameof(url));
            string id;
            if (!url.StartsWith("vbox7.com/play:"))
            {
                if (!url.StartsWith("www.vbox7.com/play:"))
                {
                    if (!url.StartsWith("www.vbox7.com/emb/external.php?vid="))
                    {
                        if (!url.StartsWith("i49.vbox7.com/player/ext.swf?")) throw new InvalidCredentialException(nameof(url));
                        id = url[34..].Split('&').First();
                        return await SearchById(id);
                    }
                    id = url[30..].Split('&').First();
                    return await SearchById(id);
                }
                id = url[19..].Split('&').First();
                return await SearchById(id);
            }
            id = url[15..].Split('&').First();
            return await SearchById(id);
        }

        private static async Task<Vbox7Object> SearchById(string id)
        {
            try
            {
                await Debug.WriteAsync($"Vbox Url is: https://www.vbox7.com/ajax/video/nextvideo.php?vid={id}");
                var call = await HttpClient.DownloadStream($"https://www.vbox7.com/ajax/video/nextvideo.php?vid={id}");
                var text = Encoding.UTF8.GetString(call.GetBuffer());
                var obj = JsonSerializer.Deserialize<Vbox7Object>(text, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
                return obj;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Exception in SearchById Vbox7: {e}");
                return null;
            }
        }
    }
}