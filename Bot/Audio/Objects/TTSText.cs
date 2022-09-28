using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Tools;
using Debug = DiscordBot.Methods.Debug;

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

        public override async Task<bool> GetAudioData(params Stream[] outputs)
        {
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
            var streamSpreader = new StreamSpreader(CancellationToken.None, outputs);
            var task = new Task(async () =>
            {
                try
                {
                    await baseStream.CopyToAsync(streamSpreader);
                    //Length = (ulong) (streamSpreader.Length / 4); Current implementation doesn't support length.
                }
                catch (Exception e)
                {
                    await Debug.WriteAsync($"TTS Reader copy task failed: \"{e}\"");
                }
            });
            task.Start();
            return true;
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