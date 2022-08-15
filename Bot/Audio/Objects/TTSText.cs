using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DiscordBot.Abstract;

namespace DiscordBot.Audio.Objects
{
    public class TtsText : PlayableItem
    {
        public enum Language
        {
            Bulgarian,
            English
        }

        private readonly string _textToSay;
        private readonly Language _ttsLanguage;

        private bool _converting;

        public TtsText(string textToSay, Language language = Language.English)
        {
            Location = "Location is used. Happy now?";
            _textToSay = textToSay;
            _ttsLanguage = language;
            Author = "Text To Speech";
            if (textToSay.Length > 3)
                switch (textToSay[..3])
                {
                    case "BG\n":
                        _ttsLanguage = Language.Bulgarian;
                        _textToSay = _textToSay[3..];
                        break;
                    case "EN\n":
                        _ttsLanguage = Language.English;
                        _textToSay = _textToSay[3..];
                        break;
                }

            Title = _textToSay.Length > 40 ? _textToSay[..40] : _textToSay;
            Title = Title.Replace('\n', ' ');
        }

        public MemoryStream DataStream { get; } = new();

        public override async Task GetAudioData(params Stream[] outputs)
        {
            _converting = true;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/gtts-cli",
                    Arguments =
                        $"-l {LanguageToString(_ttsLanguage)} --file -",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            await process.StandardInput.WriteLineAsync(_textToSay);
            process.StandardInput.Close();
            var baseStream = process.StandardOutput.BaseStream;
            var task = new Task(async () =>
            {
                try
                {
                    await baseStream.CopyToAsync(DataStream);
                    Length = (ulong) (DataStream.Length / 4);
                    _converting = false;
                }
                catch (Exception)
                {
                    // Ignored
                }
            });
            task.Start();
        }

        private static string LanguageToString(Language chars)
        {
            return chars switch {Language.Bulgarian => "bg", Language.English => "en", _ => "en"};
        }

        public override string GetId()
        {
            return null;
        }

        public override string GetThumbnailUrl()
        {
            return null;
        }

        public override string GetAddUrl()
        {
            return $"tts://{_textToSay}";
        }
    }
}