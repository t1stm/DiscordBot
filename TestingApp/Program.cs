using System;
using System.Threading.Tasks;
using DiscordBot.Standalone;
using TestingApp.WebSocket_Tests;

using DiscordBot;
using DiscordBot.Audio.Objects;
using DiscordBot.Playlists.Music_Storage;
using TestingApp.Search_Algorithm_Tests;
using TestingApp.StreamSpreader_Tests;

/*Bot.LoadDatabases();
MusicManager.LoadItems();
/*YoutubeOverride.UpdateOverrides();

RankOverrides.Rank();#1#
Audio.GeneratedSocketSessions.Add(new SocketSession
{
    Id = Guid.Empty,
    StartExpire = DateTime.UtcNow.AddDays(1).Ticks
});
await TestingServer.Start();

await Task.Delay(-1);*/

DataAccuracy.Test();