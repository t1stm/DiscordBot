using DiscordBot;
using DiscordBot.Audio.Platforms;
using TestingApp;

Bot.LoadDatabases();

var porting = new PortingTest();
await porting.ProcessMainFolder();

var items = await SharePlaylist.Get("Mazen Tirajiq");

if (items == null) return;

await porting.WriteToNewFileWithInfo("Мазен Тираджия", 
    "Този плейлист съдържа само най-мазните чалга парчета направени в периода 1980-2010г.", 
    "t1stm", true, items);
    
Bot.SaveDatabases();