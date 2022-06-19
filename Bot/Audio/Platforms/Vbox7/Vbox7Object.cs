using DiscordBot.Audio.Objects;

namespace DiscordBot.Audio.Platforms.Vbox7
{
    public class Vbox7Object
    {
        public bool Success { get; set; }
        public Vbox7Properties Options { get; set; }

        public Vbox7Video ToVbox7Video()
        {
            return new()
            {
                Title = Options?.Title,
                Author = Options?.Uploader,
                Length = Options?.Duration * 1000 ?? 0,
                Location = Options?.Src,
                Id = Options?.Vid
            };
        }
    }

    public class Vbox7Properties
    {
        public string Src { get; set; }
        public string Title { get; set; }
        public string Uploader { get; set; }
        public string Vid { get; set; }
        public ulong Duration { get; set; }
        public long Ago { get; set; }
    }
}