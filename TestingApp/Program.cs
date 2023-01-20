using DiscordBot;
using DiscordBot.Playlists.Music_Storage;
using TestingApp.Music_Database_Tests;

Bot.LoadDatabases();
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

//DataAccuracy.Test();

//new PlaylistTest().Test();
ExtractId3v2Images.Test();