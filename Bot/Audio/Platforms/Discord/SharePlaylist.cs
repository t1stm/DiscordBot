using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Methods;
using DiscordBot.Readers;
using DSharpPlus.Entities;

namespace DiscordBot.Audio.Platforms.Discord
{
    public static class SharePlaylist
    {
        public static async Task<List<PlayableItem>> Get(DiscordAttachment att)
        {
            var location = $"{Bot.WorkingDirectory}/Playlists/{att.FileName}";
            if (File.Exists(location)) File.Delete(location);
            await HttpClient.DownloadFile(att.Url, location);
            return await Get(att.FileName[..^5]);
        }

        public static bool Exists(string token)
        {
            return File.Exists($"{Bot.WorkingDirectory}/Playlists/{token}.batp");
        }

        public static FileStream Write(string token, IEnumerable<PlayableItem> list)
        {
            var bytes = new List<byte> {84, 7, 70, 60, 5, 34};
            foreach (var item in list)
            {
                var urlSplit = item.GetAddUrl().Split("://");
                var text = string.Join("://", urlSplit[1..]);
                byte acc = item switch
                {
                    YoutubeVideoInformation => 01,
                    SpotifyTrack => 02,
                    SystemFile => 03,
                    Vbox7Video => 05,
                    OnlineFile => 06,
                    TtsText => 07,
                    TwitchLiveStream => 08,
                    YoutubeOverride => 09,
                    _ => throw new ArgumentOutOfRangeException($"Item is not supported in: \"{nameof(list)}\"")
                };
                if (acc == 03 && urlSplit[0] == "dis-att") acc = 04;
                Encode(bytes, text, acc);
            }

            var fs = new FileStream($"{Bot.WorkingDirectory}/Playlists/{token}.batp", FileMode.Create);
            fs.Write(bytes.ToArray());
            return fs;
        }

        public static async Task<List<PlayableItem>> Get(string token)
        {
            try
            {
                var list = new List<PlayableItem>();
                var listDeserialized = Decode(token);
                foreach (var info in listDeserialized)
                {
                    if (info.OldFormat)
                    {
                        var item = OldFormatItem(info);
                        if (item == null) continue;
                        list.Add(item);
                        continue;
                    }

                    var add = await Search.Get(NewFormatSearch(info));
                    list.AddRange(add);
                }

                return list;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"Loading old playlist failed: {e}");
                return null;
            }
        }

        private static string NewFormatSearch(Info info)
        {
            return info.Id switch
            {
                01 => "yt://",
                02 => "spt://",
                03 => "file://",
                04 => "dis-att://",
                05 => "vb7://",
                06 => "onl://",
                07 => "tts://",
                08 => "twitch://",
                09 => "yt-ov://",
                _ => ""
            } + info.Information;
        }

        private static PlayableItem? OldFormatItem(Info info)
        {
            var split = info.Information.Split("&//");
            if (split.Length > 3)
                return info.Id switch
                {
                    01 => new YoutubeVideoInformation
                    {
                        YoutubeId = split[0],
                        Title = split[1],
                        Author = split[2],
                        Length = ulong.Parse(split[3]),
                        ThumbnailUrl = split[4]
                    },
                    02 => new SpotifyTrack
                    {
                        TrackId = split[0], Title = split[1], Author = split[2], Length = ulong.Parse(split[3])
                    },
                    03 => new SystemFile
                    {
                        Location = split[0],
                        Title = split[1],
                        Author = split[2],
                        Length = ulong.Parse(split[3])
                    },
                    04 => new SystemFile
                    {
                        Location = split[0], Title = split[1], Author = split[2], Length = ulong.Parse(split[3]),
                        IsDiscordAttachment = true
                    },
                    05 => new Vbox7Video
                    {
                        Location = split[0], Title = split[1], Author = split[2], Length = ulong.Parse(split[3])
                    },
                    06 => new OnlineFile {Location = split[0]},
                    _ => null
                };

            Debug.Write("Deserializing Playlist Format failed. Split not long enough.");
            throw new Exception("Split not long enough.");
        }

        private static void Encode(List<byte> bytes, string text, byte accessor)
        {
            List<byte> data = new();
            var encoding = text.All(IsAscii) ? (byte) 01 : text.All(IsBulgarianCharacter) ? (byte) 02 : (byte) 00;
            foreach (var ch in text)
            {
                if (encoding == 02)
                {
                    data.Add(EncodeBulgarian(ch));
                    continue;
                }

                var utf = Convert.ToUInt16(ch);
                if (encoding == 01)
                {
                    data.Add((byte) utf);
                    continue;
                }

                data.Add((byte) utf);
                data.Add((byte) (utf >> 8));
            }

            byte[] acc = {00, 02, encoding, accessor};
            lock (bytes)
            {
                bytes.AddRange(acc);
                bytes.AddRange(data);
                bytes.AddRange(new byte[] {00, 02});
            }
        }

        private static IEnumerable<Info> Decode(string token)
        {
            var bytes = File.ReadAllBytes($"{Bot.WorkingDirectory}/Playlists/{token}.batp");
            var oldFormat = false;
            List<Info> infos = new();
            if (bytes.Length < 5) throw new Exception("Empty or Corrupted File.");
            if (bytes[0] != 84 || bytes[1] != 7 || bytes[2] != 70 || bytes[3] != 60) return OldFormat(bytes);
            if (bytes[0] != 84 || bytes[1] != 7 || bytes[2] != 70 || bytes[3] != 60 || bytes[4] != 5 || bytes[5] != 34)
                oldFormat = true;
            for (var i = oldFormat ? 4 : 6; i < bytes.Length; i++)
            {
                if (bytes[i] != 00 || bytes[i + 1] != 02)
                    throw new Exception("Invalid or Corrupted File: Seperator check failed.");
                //Console.WriteLine($"Id is: {bytes[i+3]}");
                var encoding = bytes[i + 2];
                var index = bytes[i + 3];
                var data = "";
                switch (encoding)
                {
                    case 00:
                        for (var j = i + 4; j < bytes.Length; j += 2)
                        {
                            if (j + 2 == bytes.Length || bytes[j] == 00 && bytes[j + 1] == 02)
                            {
                                i = j + 1;
                                break;
                            }

                            data += BitConverter.ToChar(new[] {bytes[j], bytes[j + 1]});
                        }

                        break;
                    case 01:
                        for (var j = i + 4; j < bytes.Length; j++)
                        {
                            if (j + 2 == bytes.Length || bytes[j] == 00 && bytes[j + 1] == 02)
                            {
                                i = j + 1;
                                break;
                            }

                            data += (char) bytes[j];
                        }

                        break;
                    case 02:
                        for (var j = i + 4; j < bytes.Length; j++)
                        {
                            if (j + 2 == bytes.Length || bytes[j] == 00 && bytes[j + 1] == 02)
                            {
                                i = j + 1;
                                break;
                            }

                            data += DecodeBulgarian(bytes[j]);
                        }

                        break;
                }

                lock (infos)
                {
                    infos.Add(new Info
                    {
                        Id = index,
                        Information = data,
                        OldFormat = oldFormat
                    });
                }
            }

            return infos;
        }

        private static IEnumerable<Info> OldFormat(IReadOnlyList<byte> bytes)
        {
            Debug.Write("Using old format");
            List<Info> infos = new();
            if (bytes[0] != 84 || bytes[1] != 7 || bytes[2] != 70 || bytes[3] != 50) throw new Exception("Bad");
            for (var i = 4; i < bytes.Count; i++)
            {
                if (bytes[i] != 00 || bytes[i + 1] != 00) throw new Exception("Bad2");
                var index = bytes[i + 3];
                var data = "";
                for (var j = i + 4; j < bytes.Count; j += 2)
                {
                    if (j + 2 == bytes.Count || bytes[j] == 00 && bytes[j + 1] == 02)
                    {
                        i = j + 1;
                        break;
                    }

                    data += BitConverter.ToChar(new[] {bytes[j], bytes[j + 1]});
                }

                Console.WriteLine(data);
                lock (infos)
                {
                    infos.Add(new Info
                    {
                        Id = index,
                        Information = data
                    });
                }
            }

            return infos;
        }

        private static byte EncodeBulgarian(char character)
        {
            return character switch
            {
                'а' => 65,
                'б' => 66,
                'в' => 86,
                'г' => 71,
                'д' => 68,
                'е' => 69,
                'з' => 90,
                'и' => 73,
                'й' => 74,
                'к' => 75,
                'л' => 76,
                'м' => 77,
                'н' => 78,
                'о' => 79,
                'п' => 80,
                'с' => 83,
                'т' => 84,
                'у' => 85,
                'ф' => 70,
                'х' => 72,
                'ц' => 67,
                'ъ' => 89,
                'ь' => 88,
                'я' => 81,

                'А' => 97,
                'Б' => 98,
                'В' => 118,
                'Г' => 103,
                'Д' => 100,
                'Е' => 101,
                'З' => 122,
                'И' => 105,
                'Й' => 106,
                'К' => 107,
                'Л' => 108,
                'М' => 109,
                'Н' => 110,
                'О' => 111,
                'П' => 112,
                'С' => 115,
                'Т' => 116,
                'У' => 117,
                'Ф' => 102,
                'Х' => 104,
                'Ц' => 99,
                'Ъ' => 121,
                'Ь' => 120,
                'Я' => 113,
                //Special letters which cannot be translated into the terrible ascii-clone
                'ж' => 132,
                'ч' => 128,
                'ш' => 134,
                'щ' => 138,
                'Ж' => 142,
                'Ч' => 135,
                'Ш' => 143,
                'Щ' => 144,
                'ю' => 87,
                'Ю' => 119,
                _ => (byte) character
            };
        }

        private static bool IsBulgarianCharacter(char character)
        {
            const string allLetters =
                "абвгдежзийклмнопрстуфхцшщъьюяАБВГДЕЖЗИЙКЛМНОРПСТУФХЦШЩЪЬЮЯ0123456789?.,\\[];'`!@#$%^&*()_+-=~\"\'<>? ";
            return allLetters.Contains(character);
        }

        private static bool IsAscii(char character)
        {
            const string allLetters =
                "qazwsxedcrfvtgbyhnujmikolpQAZWSXEDCRFVTGBYHNUJMIKOLP?.,\\[];'`!@#$%^&*()_+-=~\"\'<>? ";
            return allLetters.Contains(character);
        }

        private static char DecodeBulgarian(byte data)
        {
            return data switch
            {
                65 => 'а',
                66 => 'б',
                86 => 'в',
                71 => 'г',
                68 => 'д',
                69 => 'е',
                90 => 'з',
                73 => 'и',
                74 => 'й',
                75 => 'к',
                76 => 'л',
                77 => 'м',
                78 => 'н',
                79 => 'о',
                80 => 'п',
                83 => 'с',
                84 => 'т',
                85 => 'у',
                70 => 'ф',
                72 => 'х',
                67 => 'ц',
                89 => 'ъ',
                88 => 'ь',
                81 => 'я',

                97 => 'А',
                98 => 'Б',
                118 => 'В',
                103 => 'Г',
                100 => 'Д',
                101 => 'Е',
                122 => 'З',
                105 => 'И',
                106 => 'Й',
                107 => 'К',
                108 => 'Л',
                109 => 'М',
                110 => 'Н',
                111 => 'О',
                112 => 'П',
                115 => 'С',
                116 => 'Т',
                117 => 'У',
                102 => 'Ф',
                104 => 'Х',
                99 => 'Ц',
                121 => 'Ъ',
                120 => 'Ь',
                113 => 'Я',
                //Special letters
                132 => 'ж',
                128 => 'ч',
                134 => 'ш',
                138 => 'щ',
                142 => 'Ж',
                135 => 'Ч',
                143 => 'Ш',
                144 => 'Щ',
                87 => 'ю',
                119 => 'Ю',
                _ => (char) data
            };
        }

        public struct Info
        {
            public byte Id;
            public string Information;
            public bool OldFormat;
        }
    }
}