using DiscordBot;
using DiscordBot.Playlists.Music_Storage;
using TestingApp;
using TestingApp.Semaphore_Tests;

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

await DelayedRelease.Test();

/*var id3v2_tag = Id3v2.GetImageFromTag("/nvme0/DiscordBot/Music Database/Chalga/Desi/Деси - Иди си.mp3");

Console.WriteLine(id3v2_tag);*/

