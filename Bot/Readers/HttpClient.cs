using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BatToshoRESTApp.Methods;
using PuppeteerSharp;

namespace BatToshoRESTApp.Readers
{
    public static class HttpClient
    {
        public static readonly string CookieDestination = $"{Bot.WorkingDirectory}/cookies.txt";

        public static System.Net.Http.HttpClient WithCookies()
        {
            var container = new CookieContainer();
            var file = CookieDestination;
            Debug.Write($"File is: {file}");
            var collection = new CookieCollection();
            var cl = ParseFileAsCookies(file);
            foreach (var cook in cl) collection.Add(cook);
            container.Add(collection);
            var handler = new HttpClientHandler {UseCookies = true, CookieContainer = container};
            return new System.Net.Http.HttpClient(handler);
        }

        public static async Task<string> DownloadFile(string url, string location, bool withCookies = true)
        {
            var client = withCookies ? WithCookies() : new System.Net.Http.HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var send = await client.SendAsync(request);
            var response = await send.Content.ReadAsStreamAsync();
            var fs = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Read);
            await response.CopyToAsync(fs);
            return location;
        }

        private static IEnumerable<Cookie> ParseFileAsCookies(string yes)
        {
            var cookies = new List<Cookie>();
            var lineCount = 0;
            foreach (var line in File.ReadAllLines(yes))
            {
                if (line.StartsWith("#") || line == "") continue;
                lineCount++;
                var parts = line.Split('	');
                if (parts.Length != 7)
                    throw new FormatException($"Line {lineCount} has {parts.Length} columns. Expected 7");
                var domain = parts[0];
                var path = parts[2];
                var secure = parts[3] == "TRUE";
                var expires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[4])).UtcDateTime;
                var name = parts[5];
                var value = parts[6];
                var cookie = new Cookie(name, value, path, domain)
                {
                    Secure = secure,
                    Expires = expires
                };

                cookies.Add(cookie);
            }

            return cookies;
        }

        public static async Task<string> GetSourceCodeAfterLoadingPage(string uri)
        {
            await Debug.WriteAsync($"URI is: {uri}");
            /*var options = new ChromeOptions
            {
                BinaryLocation = "/usr/bin/chromium-dev"
            };
            options.AddArguments(new List<string> { "headless", "disable-gpu" });
            var browser = new ChromeDriver(options);
            browser.Navigate().GoToUrl($"https://vbox7.com{uri}"); // Excellent url usage.
            return browser.PageSource;*/
            //24.11.2021, 19:34: Old code that was supposed to work but didn't. Yes that's how the world works. I should probably delete the other api.... Fuck it I am too lazy to open nuget now.
            //Update 19:36: After some thinking that lasted for at least three whole seconds, I decided to move this method from the Vbox7Video class, here.
            var options = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = "/usr/bin/chromium-dev"
            };
            var browser = await Puppeteer.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            await page.GoToAsync(uri);
            var source = await page.GetContentAsync();
            await browser.CloseAsync();
            return source;
        }
    }
}