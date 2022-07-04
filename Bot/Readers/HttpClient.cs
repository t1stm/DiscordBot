using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DiscordBot.Readers
{
    public static class HttpClient
    {
        public static readonly string[] CookieDestinations = {$"{Bot.WorkingDirectory}/cookies.txt"};

        public static System.Net.Http.HttpClient WithCookies()
        {
            var container = new CookieContainer();
            var collection = new CookieCollection();
            var cl = ParseFileAsCookies(CookieDestinations.GetRandom());
            foreach (var cook in cl) collection.Add(cook);
            container.Add(collection);
            var handler = new HttpClientHandler {UseCookies = true, CookieContainer = container};
            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3554.0 Safari/537.36");
            return client;
        }

        public static async Task<string> DownloadFile(string url, string location, bool withCookies = true,
            bool chunked = true)
        {
            var client = withCookies ? WithCookies() : new System.Net.Http.HttpClient();
            var response = new MemoryStream();
            switch (chunked)
            {
                case false:
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var send = await client.SendAsync(request);
                    var resp = await send.Content.ReadAsStreamAsync();
                    await resp.CopyToAsync(response);
                    break;
                case true:
                    await ChunkedDownloaderToStream(client, new Uri(url), response);
                    break;
            }

            response.Position = 0;
            var fs = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Read);
            await response.CopyToAsync(fs);
            return location;
        }

        public static async Task<MemoryStream> DownloadStream(string url, bool withCookies = true)
        {
            var client = withCookies ? WithCookies() : new System.Net.Http.HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var send = await client.SendAsync(request);
            var response = await send.Content.ReadAsStreamAsync();
            var ms = new MemoryStream();
            await response.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
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

        private static async Task<long?> GetContentLengthAsync(System.Net.Http.HttpClient httpClient, string requestUri,
            bool ensureSuccess = true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, requestUri);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (ensureSuccess)
                response.EnsureSuccessStatusCode();
            return response.Content.Headers.ContentLength;
        }

        public static async Task ChunkedDownloaderToStream(System.Net.Http.HttpClient httpClient, Uri uri,
            params Stream[] streams)
        {
            var fileSize = await GetContentLengthAsync(httpClient, uri.AbsoluteUri) ?? 0;
            const long chunkSize = 10485760;
            if (fileSize == 0) throw new Exception("File has no content");
            var segmentCount = (int) Math.Ceiling(1.0 * fileSize / chunkSize);
            for (var i = 0; i < segmentCount; i++)
            {
                var from = i * chunkSize;
                var to = (i + 1) * chunkSize - 1;
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Range = new RangeHeaderValue(from, to);

                // Download Stream
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                    response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[81920];
                int bytesCopied;
                do
                {
                    bytesCopied = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                    foreach (var output in streams) await output.WriteAsync(buffer.AsMemory(0, bytesCopied));
                } while (bytesCopied > 0);
            }
        }
    }
}