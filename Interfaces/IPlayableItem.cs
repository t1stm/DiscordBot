using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Objects
{
    public interface IPlayableItem
    {
        string GetName();
        ulong GetLength();
        string GetLocation();
        Task Download();
        void SetRequester(DiscordMember user);
        DiscordMember GetRequester();
        string GetId();
        string GetTypeOf();
        bool GetIfErrored();
        string GetTitle();
        string GetAuthor();
        string GetThumbnailUrl();
    }
}