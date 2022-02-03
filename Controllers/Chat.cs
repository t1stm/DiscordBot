using WebSocketSharper;
using WebSocketSharper.Server;
using static System.String;

namespace BatToshoRESTApp.Controllers
{
    public class Chat : WebSocketBehavior
    {
        private readonly string _suffix;

        public Chat ()
            : this (null)
        {
            
        }

        public Chat (string suffix)
        {
            _suffix = suffix ?? Empty;
        }

        protected override void OnMessage (MessageEventArgs e)
        {
            Sessions.Broadcast (e.Data + _suffix);
        }
    }
}