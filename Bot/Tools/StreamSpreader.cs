using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Tools
{
    public class StreamSpreader
    {
        private Stream Source { get; }
        private FeedableStream[] Destinations { get; }
        private bool StopCopying { get; set; }
        private CancellationToken Token { get; set; }

        public StreamSpreader(Stream source, CancellationToken token, params Stream[] destinations)
        {
            Source = source;
            Destinations = destinations.Select(r => new FeedableStream(r)).ToArray();
            Token = token;
        }

        public async Task Start()
        {
            var buffer = new Memory<byte>();

            int read;

            while (!StopCopying && !Token.IsCancellationRequested && (read = await Source.ReadAsync(buffer, Token)) > 0)
            {
                
            }
        }

        public void Stop()
        {
            StopCopying = true;
        }
    }
}