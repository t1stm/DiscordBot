#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Readers;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DiscordBot
{
    public class CommandsSpecific : ApplicationCommandModule
    {
        [SlashCommand("givecontent", "This command gives you special content. rev 39")]
        public async Task ContentCommand(InteractionContext ctx, [Option("id", "The id of the content")]
            long? id = default)
        {
            try
            {
                await ctx.CreateResponseAsync("```Hello!```");
                var dirs = Directory.EnumerateDirectories("/hdd0/fakku").ToList();
                if (id is null or < 1 || id > dirs.Count - 1) id = new Random().Next(1, dirs.Count - 2);
                var loc = (int) id;
                var files = Directory.GetFiles(dirs.First(d => d.EndsWith(loc + "")), "*.png").OrderBy(r =>
                {
                    var yes = int.Parse(r.Split("/").Last().Split(".")[0]);
                    return yes;
                }).ToArray();
                var json = JsonSerializer.Deserialize<JsonStructure>(
                    await File.ReadAllTextAsync($"/hdd0/fakku/{id}/info.json"));
                var dic = new Dictionary<string, Stream>();
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"```{json.Title} - {json.Author} - {json.Event}\nLength: {json.Length} pages | Id: {id}```"));
                for (var i = 0; i < files.Length; i++)
                {
                    if (dic.Count >= 10)
                    {
                        await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent($"```Pages: {i - 9} - {i}```").WithFiles(dic));
                        foreach (var el in dic.Values) await el.DisposeAsync();
                        dic = new Dictionary<string, Stream>();
                        continue;
                    }

                    dic.Add(files[i].Split("/").Last(), File.OpenRead(files[i]));
                }

                if (dic.Count > 0)
                    await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"```Pages: {files.Length - dic.Count} - {files.Length}```").WithFiles(dic));
                foreach (var el in dic.Values) await el.DisposeAsync();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"GiveContent Slash Command Failed: {e}");
            }
        }

        [SlashCommand("r34", "This command searches the rule. rev 39")]
        public async Task RuleCommand(InteractionContext ctx, [Option("searchterm", "Search Term")] string search,
            [Option("results", "Number of results. Must be < 1000")]
            long? results)
        {
            try
            {
                //https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags=term_here_without_spaces&limit=20 Example Url Request
                //https://rule34.xxx/index.php?page=post&s=view&id=5553819 Example Page Url
                await ctx.CreateResponseAsync("```Hello!```");
                var transf = search.Replace("&", "");
                var httpResp = await HttpClient.DownloadStream(
                    $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={transf}&limit={results ?? 20}");
                var json = Encoding.UTF8.GetString(httpResp.GetBuffer());
                if (string.IsNullOrEmpty("json") || json == "[]")
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("```No results found```"));
                    return;
                }

                RuleObject[]? rule;
                try
                {
                    rule = JsonSerializer.Deserialize<RuleObject[]>(json);
                }
                catch (Exception)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("```No results found```"));
                    return;
                }

                if (rule == null) return;
                if (rule.Length < 1)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("```No results found```"));
                    return;
                }

                var resp = $"```Results for \"{search}\":```";
                var a = 0;
                foreach (var o in rule)
                {
                    a++;
                    resp = $"{resp}\n{o.file_url}";
                    if (a <= 4) continue;
                    a = 0;
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(resp));
                    await Task.Delay(2500);
                    resp = "";
                }

                if (a != 0) await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(resp));
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("```Finished Sending```"));
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"r34 Slash Command Failed: {e}");
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // Yes I do like these Rider IDE comments.
        private class RuleObject
        {
            public string? preview_url { get; init; }
            public string? sample_url { get; init; }
            public string? file_url { get; init; }
            public int directory { get; init; }
            public string? hash { get; init; }
            public int width { get; init; }
            public int height { get; init; }
            public int id { get; init; }
            public string? image { get; init; }
            public int change { get; init; }
            public string? owner { get; init; }
            public int parent_id { get; init; }
            public string? rating { get; init; }
            public int sample { get; init; }
            public int sample_height { get; init; }
            public int sample_width { get; init; }
            public int score { get; init; }
            public string? tags { get; init; }
        }

        private struct JsonStructure
        {
            public string Title { get; init; }
            public string Author { get; init; }
            public string Event { get; init; }
            public int Length { get; init; }

            public string Origins { get; init; }
        }
    }
}