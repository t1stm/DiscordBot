using System.Threading.Tasks;
using BatToshoRESTApp.Abstract;

namespace BatToshoRESTApp.Audio.Objects
{
    public class Vbox7Video : PlayableItem
    {
        public override Task Download()
        {
            return Task.CompletedTask;
        }

        public override string GetThumbnailUrl()
        {
            return null;
        }

        public new bool GetIfErrored()
        {
            return false;
        }

        public override string GetId()
        {
            return null;
        }

        public override string GetTypeOf()
        {
            return "Vbox7 Video";
        }
    }
}