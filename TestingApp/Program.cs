using System;
using DiscordBot;
using TestingApp;

Bot.LoadDatabases();

/*var porting = new PortingTest();

var items = await SharePlaylist.Get("Mazen Tirajiq");

if (items == null) return;

await porting.WriteToNewFileWithInfo("Мазен Тираджия", 
    "Този плейлист съдържа само най-мазните парчета направени в периода 1980-2010г.", 
    "t1stm", true, items);
    
Bot.SaveDatabases();*/

try
{
    //await new EditPlaylist().Execute();
    await new SaveEditedPlaylist().Execute();
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}