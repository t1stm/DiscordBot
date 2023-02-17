using DiscordBot;
using DiscordBot.Playlists.Music_Storage;

Bot.LoadDatabases();
MusicManager.Load();
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