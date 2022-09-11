using DiscordBot.Data.Models;

namespace DiscordBot.Data
{
    public static class Databases
    {
        public static readonly Manager<FuckYoutubeModel> FuckYoutubeDatabase = new();
        public static readonly Manager<GuildsModel> GuildDatabase = new();
        public static readonly Manager<UsersModel> UserDatabase = new();
        public static readonly Manager<VideoInformationModel> VideoDatabase = new();
    }
}