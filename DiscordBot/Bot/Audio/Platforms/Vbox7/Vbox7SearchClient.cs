#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Abstract.Errors;
using DiscordBot.Methods;
using DiscordBot.Readers;

namespace DiscordBot.Audio.Platforms.Vbox7
{
    public static class Vbox7SearchClient
    {
        // I TAKA NEKA SLEDVASHTIQ MUZIKALEN POZDRAV ZVUCHI SPECIALNO,
        // ZA MOQ BRAT SASHO.... SKORPIONA,
        // ZA RUSI SHAMPIONA,
        // ZA IHTI DRAKONA,
        // ZA MOITE PRIQTELI ANDI BAR I DOLNA METROPOLIA,
        // ZA GALI, ZA SHUREKA,
        // ZA KALI,
        // ZA CQLATA KOMPANIA,
        // ZA NASHIQ... ZAPISVACH,
        // D.. ZA DJ MECHO TESKIQ,
        // HA HA HA Ha ha ha
        // https://www.vbox7.com/play:a3769b5b6b - Suraikata 2014 mix
        public static async IAsyncEnumerable<Result<Vbox7Object, Error>> GetResultsFromSearch(string term)
        {
            var call = await HttpClient.DownloadStream($"https://www.vbox7.com/search?vbox_q={term}");
            var yes = Encoding.UTF8.GetString(call.GetBuffer());
            var yes1 = yes.Split(@"class=""info-cont""")[1].Split("\n");
            foreach (var line in yes1)
            {
                if (!line.Contains(@"a href=""/play")) continue;
                yield return await SearchById(line.Split("/play:")[1].Split("\"")[0]);
            }
        }

        public static async Task<Result<Vbox7Object, Error>> SearchUrl(string url)
        {
            if (url.Length < 7) throw new InvalidCredentialException(nameof(url));
            await Debug.WriteAsync($"Vbox SearchUrl: {url}");
            url = url.StartsWith("http://") ? url[7..] :
                url.StartsWith("https://") ? url[8..] : url;
            if (url == "") throw new InvalidCredentialException(nameof(url));
            string id;
            if (!url.StartsWith("vbox7.com/play:"))
            {
                if (!url.StartsWith("www.vbox7.com/play:"))
                {
                    if (!url.StartsWith("www.vbox7.com/emb/external.php?vid="))
                    {
                        if (!url.StartsWith("i49.vbox7.com/player/ext.swf?"))
                            return Result<Vbox7Object, Error>.Error(new Vbox7Error());
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

        private static async Task<Result<Vbox7Object, Error>> SearchById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id)) return null;
                await Debug.WriteAsync($"Vbox Url is: https://www.vbox7.com/ajax/video/nextvideo.php?vid={id}");
                var call = await HttpClient.DownloadStream($"https://www.vbox7.com/ajax/video/nextvideo.php?vid={id}");
                var text = Encoding.UTF8.GetString(call.GetBuffer());
                return Result<Vbox7Object, Error>.Success(JsonSerializer.Deserialize<Vbox7Object>(text,
                    new JsonSerializerOptions {PropertyNameCaseInsensitive = true}));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Exception in SearchById Vbox7: {e}");
                return Result<Vbox7Object, Error>.Error(new Vbox7Error());
            }
        }
    }
}