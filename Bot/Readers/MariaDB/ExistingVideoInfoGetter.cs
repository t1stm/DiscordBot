using System;
using System.Linq;
using System.Threading.Tasks;
using BatToshoRESTApp.Audio.Objects;
using BatToshoRESTApp.Methods;
using MySql.Data.MySqlClient;

namespace BatToshoRESTApp.Readers.MariaDB
{
    public class ExistingVideoInfoGetter : IBaseMariaDatabase
    {
        private const string Database = "SERVER=localhost;DATABASE=data;UID=root;PASSWORD=123;SSL Mode=None;";

        public async Task<YoutubeVideoInformation> Read(string id)
        {
            try
            {
                var connection = new MySqlConnection(Database);
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

        public async Task Add(YoutubeVideoInformation videoInfo)
        {
            try
            {
                var read = await Read(videoInfo.YoutubeId);
                if (read != null) return;

                MySqlConnection connection = new(Database);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    "INSERT INTO videoinformation (videoid,title,author,length,thumbnail) " +
                    $"VALUES (\"{videoInfo.YoutubeId}\", " +
                    $"\"{videoInfo.Title.Replace("\"", "\"\"")}\", " +
                    $"\"{videoInfo.Author.Replace("\"", "\"\"")}\", " +
                    $"\"{videoInfo.Length}\"," +
                    $"\"{videoInfo.ThumbnailUrl.Split("?").First()}\")", connection);
                cmd.ExecuteNonQuery();
                await connection.CloseAsync();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Write error: {e}");
            }
        }
    }
}