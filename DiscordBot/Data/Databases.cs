using DatabaseManager;
using DiscordBot.Data.Models;
using DiscordBot.Methods;

namespace DiscordBot.Data;

public static class Databases
{
    public static readonly DatabaseSettings Settings = new($"{Bot.WorkingDirectory}/Databases/")
    {
        LogAction = log =>
        {
            if (Bot.DebugMode)
                Debug.Write($"[Database]: {log}");
        }
    };

    public static readonly DatabaseManager<FuckYoutubeModel> FuckYoutubeDatabase = new(Settings);
    public static readonly DatabaseManager<GuildsModel> GuildDatabase = new(Settings);
    public static readonly DatabaseManager<UsersModel> UserDatabase = new(Settings);
    public static readonly DatabaseManager<VideoInformationModel> VideoDatabase = new(Settings);
    public static readonly DatabaseManager<VotesModel> VotesDatabase = new(Settings);
}