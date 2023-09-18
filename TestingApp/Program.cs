using DiscordBot;
using DiscordBot.Playlists.Music_Storage;
using TestingApp;

Bot.LoadDatabases();
MusicManager.Load();

/*var id3v2_tag = Id3v2.GetImageFromTag("/nvme0/DiscordBot/Music Database/Chalga/Desi/Деси - Иди си.mp3");

Console.WriteLine(id3v2_tag);*/

await PlaylistTest.Test();
