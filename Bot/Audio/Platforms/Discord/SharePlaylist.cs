using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Methods;
using BatToshoRESTApp.Readers;
using DSharpPlus.Entities;

namespace BatToshoRESTApp.Audio.Platforms.Discord
{
    public static class SharePlaylist
    {
        public static async Task<List<IPlayableItem>> Get(DiscordAttachment att)
        {
            var location = $"{Bot.WorkingDirectory}/Playlists/{att.FileName}";
            if (File.Exists(location)) File.Delete(location);
            await HttpClient.DownloadFile(att.Url, location);
            return Get(att.FileName[..^5]);
        }

        public static List<IPlayableItem> Get(string token)
        {
            try
            {
                var list = new List<IPlayableItem>();
                var listDeserialized = Deserialize(token);
                foreach (var info in listDeserialized)
                {
                    var split = info.Information.Split("&//");
                    if (split.Length <= 3) Debug.Write("Deserializing Playlist Format failed. Split not long enough.");
                    switch (info.Id)
                    {
                        case 01:
                            list.Add(new YoutubeVideoInformation
                            {
                                YoutubeId = split[0],
                                Title = split[1],
                                Author = split[2],
                                Length = ulong.Parse(split[3]),
                                ThumbnailUrl = split[4]
                            });
                            break;
                        case 02:
                            list.Add(new SpotifyTrack
                            {
                                TrackId = split[0],
                                Title = split[1],
                                Author = split[2],
                                Length = ulong.Parse(split[3])
                            });
                            break;
                        case 03:
                            list.Add(new SystemFile
                            {
                                Location = split[0],
                                Title = split[1],
                                Author = split[2],
                                Length = ulong.Parse(split[3]),
                                IsDiscordAttachment = true
                            });
                            break;
                        case 04:
                            list.Add(new SystemFile
                            {
                                Location = split[0],
                                Title = split[1],
                                Author = split[2],
                                Length = ulong.Parse(split[3])
                            });
                            break;
                        case 05:
                            list.Add(new Vbox7Video
                            {
                                Location = split[0],
                                Title = split[1],
                                Author = split[2],
                                Length = ulong.Parse(split[3])
                            });
                            break;
                        case 06:
                            list.Add(new OnlineFile
                            {
                                Url = split[0]
                            });
                            break;
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                Debug.Write($"Loading old playlist failed: {e}");
                return null;
            }
        }

        public static FileStream Write(string token, IEnumerable<IPlayableItem> list)
        {
            var bytes = new List<byte> {84, 7, 70, 60};
            foreach (var item in list)
                switch (item.GetTypeOf())
                {
                    case "Youtube Video":
                        Serialize(bytes,
                            $"{item.GetId()}&//{item.GetTitle()}&//{item.GetAuthor()}&//{item.GetLength()}&//{item.GetThumbnailUrl()}",
                            01);
                        break;
                    case "Spotify Track":
                        Serialize(bytes,
                            $"{item.GetId()}&//{item.GetTitle()}&//{item.GetAuthor()}&//{item.GetLength()}", 02);
                        break;
                    case "Discord Attachment":
                        Serialize(bytes,
                            $"{item.GetLocation()}&//{item.GetTitle()}&//{item.GetAuthor()}&//{item.GetLength()}", 03);
                        break;
                    case "Local File":
                        Serialize(bytes,
                            $"{item.GetLocation()}&//{item.GetTitle()}&//{item.GetAuthor()}&//{item.GetLength()}", 04);
                        break;
                    case "Vbox7 Video":
                        Serialize(bytes,
                            $"{item.GetLocation()}&//{item.GetTitle()}&//{item.GetAuthor()}&//{item.GetLength()}", 05);
                        break;
                    case "Online File":
                        Serialize(bytes,
                            $"{item.GetLocation()}&//{item.GetTitle()}&//{item.GetAuthor()}&//{item.GetLength()}", 06);
                        break;
                }

            var fs = new FileStream($"{Bot.WorkingDirectory}/Playlists/{token}.batp", FileMode.Create);
            fs.Write(bytes.ToArray());
            return fs;
        }

        private static void Serialize(List<byte> bytes, string text, byte accessor)
        {
            List<byte> data = new();
            var encoding = text.All(IsBulgarianCharacter) ? (byte) 02 : text.All(IsAscii) ? (byte) 01 : (byte) 00;
            foreach (var ch in text)
            {
                if (encoding == 02)
                {
                    data.Add(EncodeBulgarian(ch));
                    continue;
                }

                var utf = Convert.ToUInt16(ch);
                if (encoding == 01) data.Add((byte) utf);
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

        private static IEnumerable<Info> Deserialize(string token)
        {
            var bytes = File.ReadAllBytes($"{Bot.WorkingDirectory}/Playlists/{token}.batp");
            List<Info> infos = new();
            if (bytes[0] != 84 || bytes[1] != 7 || bytes[2] != 70 || bytes[3] != 60) return OldFormat(token);
            for (var i = 4; i < bytes.Length; i++)
            {
                if (bytes[i] != 00 || bytes[i + 1] != 02) throw new Exception("Bad2");
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
                        Information = data
                    });
                }
            }

            return infos;
        }

        private static IEnumerable<Info> OldFormat(string token)
        {
            Console.WriteLine("Using old format");
            var bytes = File.ReadAllBytes($"{Bot.WorkingDirectory}/Playlists/{token}.batp");
            List<Info> infos = new();
            if (bytes[0] != 84 || bytes[1] != 7 || bytes[2] != 70 || bytes[3] != 50) throw new Exception("Bad");
            for (var i = 4; i < bytes.Length; i++)
            {
                if (bytes[i] != 00 || bytes[i + 1] != 00) throw new Exception("Bad2");
                var index = bytes[i + 3];
                var data = "";
                for (var j = i + 4; j < bytes.Length; j += 2)
                {
                    if (j + 2 == bytes.Length || bytes[j] == 00 && bytes[j + 1] == 02)
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

        private struct Info
        {
            public byte Id;
            public string Information;
        }
    }
}