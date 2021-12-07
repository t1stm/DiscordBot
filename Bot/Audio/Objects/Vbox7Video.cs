using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Objects
{
    public class Vbox7Video : IPlayableItem
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public ulong Length { get; set; }
        public string Location { get; set; }
        private DiscordMember Requester { get; set; }

        public string GetName()
        {
            return $"{Title}{Author switch {null => "", _ => $" - {Author}"}}";
        }

        public ulong GetLength()
        {
            return Length;
        }

        public string GetLocation()
        {
            return Location;
        }

        public Task Download()
        {
            return Task.CompletedTask;
        }

        public string GetTitle()
        {
            return Title;
        }

        public string GetAuthor()
        {
            return Author;
        }

        public string GetThumbnailUrl()
        {
            return null;
        }

        public bool GetIfErrored()
        {
            return false;
        }

        public void SetRequester(DiscordMember user)
        {
            Requester = user;
        }

        public DiscordMember GetRequester()
        {
            return Requester;
        }

        public string GetId()
        {
            return null;
        }

        public string GetTypeOf()
        {
            return "Vbox7 Video";
        }
    }
}