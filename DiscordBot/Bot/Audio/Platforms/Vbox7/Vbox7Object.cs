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
}