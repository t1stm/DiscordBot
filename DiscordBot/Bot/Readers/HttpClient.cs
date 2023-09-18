using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DiscordBot.Abstract.Errors;
using DiscordBot.Methods;
using Result;
using Result.Objects;
using Streams;

namespace DiscordBot.Readers;

public static class HttpClient
{
    public static readonly string[] CookieDestinations = { $"{Bot.WorkingDirectory}/cookies3.txt" };

    public static System.Net.Http.HttpClient WithCookies()
    {
        var container = new CookieContainer();
        var collection = new CookieCollection();
        var cl = ParseFileAsCookies(CookieDestinations.GetRandom());
        foreach (var cook in cl) collection.Add(cook);
        container.Add(collection);
        var handler = new HttpClientHandler { UseCookies = true, CookieContainer = container };
        var client = new System.Net.Http.HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/117.0");
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
                var stream_spreader = await ChunkedDownloader(client, new Uri(url));
                if (stream_spreader == Status.Error) return "";
                var result = stream_spreader.GetOK();

                result.AddDestination(response);

                // TODO: Finish this.
                break;
        }

        response.Position = 0;
        var fs = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Read);
        await response.CopyToAsync(fs);
        return location;
    }

    public static async Task<Stream> OpenAsStream(string url, bool withCookies = true)
    {
        var client = withCookies ? WithCookies() : new System.Net.Http.HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var send = await client.SendAsync(request);
        return await send.Content.ReadAsStreamAsync();
    }

    public static async Task<MemoryStream> DownloadStream(string url, bool withCookies = true)
    {
        var client = withCookies ? WithCookies() : new System.Net.Http.HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var send = await client.SendAsync(request);
        await using var response = await send.Content.ReadAsStreamAsync();
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
                throw new FormatException($"Line: ({lineCount}) has {parts.Length} columns. Expected 7");
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

    public static async Task<Result<StreamSpreader, Error>> ChunkedDownloader(
        System.Net.Http.HttpClient httpClient, Uri uri, bool autoFinish = false)

    {
        var stream_spreader = new StreamSpreader
        {
            IsAsynchronous = true,
            KeepCached = true
        };

        var fileSize = await GetContentLengthAsync(httpClient, uri.AbsoluteUri) ?? 0;
        const long chunkSize = 10485760;

        if (fileSize == 0) return Result<StreamSpreader, Error>.Error(new UnknownError());

        async void DownloadAction()
        {
            try
            {
                var segment_count = (int)Math.Ceiling(1.0 * fileSize / chunkSize);
                for (var i = 0; i < segment_count; i++)
                {
                    var from = i * chunkSize;
                    var to = (i + 1) * chunkSize - 1;
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Range = new RangeHeaderValue(from, to);

                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode) response.EnsureSuccessStatusCode();
                    var stream = await response.Content.ReadAsStreamAsync();
                    var buffer = new byte[1 << 16];

                    int bytes_copied;
                    do
                    {
                        bytes_copied = await stream.ReadAsync(buffer);
                        stream_spreader.Write(buffer, 0, bytes_copied);
                    } while (bytes_copied > 0);
                }

                if (autoFinish) stream_spreader.FinishWriting();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Download Action in Chunked Downloader failed: \'{e}\'");
            }
        }

        var download_action = new Task(DownloadAction);
        download_action.Start();

        return Result<StreamSpreader, Error>.Success(stream_spreader);
    }
}