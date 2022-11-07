using System;
using System.Threading.Tasks;
using DiscordBot.Standalone;
using TestingApp.WebSocket_Tests;

using DiscordBot;
using DiscordBot.Audio.Objects;
using TestingApp.Search_Algorithm_Tests;

Bot.LoadDatabases();
YoutubeOverride.UpdateOverrides();

RankOverrides.Rank();
/*Audio.GeneratedSocketSessions.Add(new SocketSession
{
    Id = Guid.Empty,
    StartExpire = DateTime.UtcNow.AddDays(1).Ticks
});
await TestingServer.Start();

await Task.Delay(-1);*/