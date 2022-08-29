using System;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Audio.Objects;
using DiscordBot.Methods;
using MySql.Data.MySqlClient;

namespace DiscordBot.Readers.MariaDB
{
    public static class ExistingVideoInfoGetter
    {
        public static async Task<YoutubeVideoInformation> Read(string id)
        {
            try
            {
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    $"SELECT * FROM videoinformation WHERE videoid = \"{id}\"", connection);
                var dataReader = cmd.ExecuteReader();
                var vi = new YoutubeVideoInformation();
                while (await dataReader.ReadAsync())
                    vi = new YoutubeVideoInformation
                    {
                        YoutubeId = dataReader["videoid"] + "",
                        Title = dataReader["title"] + "",
                        Author = dataReader["author"] + "",
                        Length = ulong.Parse(dataReader["length"] + ""),
                        ThumbnailUrl = dataReader["thumbnail"] + ""
                    };
                await dataReader.CloseAsync();
                await connection.CloseAsync();
                if (!string.IsNullOrEmpty(vi.Title) || !string.IsNullOrEmpty(vi.YoutubeId) ||
                    !string.IsNullOrEmpty(vi.Author)) return vi;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Read error: {e}");
            }

            return null;
        }

        public static async Task Add(YoutubeVideoInformation videoInfo)
        {
            try
            {
                var read = await Read(videoInfo.YoutubeId);
                if (read != null) return;

                MySqlConnection connection = new(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    "INSERT INTO videoinformation (videoid,title,author,length,thumbnail) " +
                    $"VALUES (\"{videoInfo.YoutubeId}\", " +
                    $"\"{videoInfo.Title.Replace("\"", "\"\"").Replace("\\", "\\\\")}\", " +
                    $"\"{videoInfo.Author.Replace("\"", "\"\"").Replace("\\", "\\\\")}\", " +
                    $"\"{videoInfo.Length}\"," +
                    $"\"{videoInfo.ThumbnailUrl.Split("?").First()}\")", connection);
                await cmd.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Write error: {e}");
            }
        }
    }
}