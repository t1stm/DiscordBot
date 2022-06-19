using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using Swan;

namespace DiscordBot.Miscellaneous
{
    public class Vrutkane
    {
        public Vrutkane(DiscordClient client, DiscordGuild guild, DiscordChannel channel)
        {
            Client = client;
            Guild = guild;
            Channel = channel;
        }

        private DiscordClient Client { get; }
        private DiscordGuild Guild { get; }
        private DiscordChannel Channel { get; }

        private Timer Timer { get; } = new();
        private bool Running { get; set; }

        public void Toggle()
        {
            if (Client == null || Guild == null || Channel == null) return;
            if (Running) return;
            Running = true;
            Timer.Elapsed += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            TimerOnElapsed().Await();
        }

        private async Task TimerOnElapsed()
        {
            var list = new List<DiscordMessage>();
            var old = await Client.SendMessageAsync(Channel, "$wa");
            var message = new List<DiscordMessage>();
            for (var i = 0; i < 10; i++)
            {
                while (message.Count == 0)
                {
                    message = (List<DiscordMessage>) await Channel.GetMessagesAfterAsync(old.Id);
                    message = message.Where(mess => mess.Author.IsBot).ToList();
                }

                list.AddRange(message);
            }

            // DiscordMessage choice;
            //
            // foreach (var VARIABLE in COLLECTION)
            // {
            //     
            // }
        }
    }
}