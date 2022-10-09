using DiscordBot.Data.Models;

namespace DiscordBot.Data
{
    public static class Databases
    {
        public static readonly DatabaseManager<FuckYoutubeModel> FuckYoutubeDatabase = new();
        public static readonly DatabaseManager<GuildsModel> GuildDatabase = new();
        public static readonly DatabaseManager<UsersModel> UserDatabase = new();
        public static readonly DatabaseManager<VideoInformationModel> VideoDatabase = new();
        public static readonly DatabaseManager<PlaylistsModel> PlaylistDatabase = new();
        public static readonly DatabaseManager<PlaylistThumbnailsModel> PlaylistThumbnailDatabase = new();
    }
}