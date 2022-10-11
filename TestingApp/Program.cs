using DiscordBot;
using TestingApp;

Bot.LoadDatabases();

for (int i = 0; i < 50; i++)
{
    new PlaylistTest().Test();
}