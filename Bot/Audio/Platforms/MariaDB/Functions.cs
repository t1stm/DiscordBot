using System;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Bat_Tosho.Audio.Platforms.MariaDB
{
    public static class Functions //Oh yes the new brand spanking feature that will make the bot 10* faster and remove the Youtube dependency for a part.
    {
        private const string DbSett = "SERVER=localhost;DATABASE=data;UID=root;PASSWORD=123;SSL Mode=None;";
        public struct VideoInformation
        {
            public string VideoId;
            public string Title;
            public string Author;
            public int LengthMs;
            public string Thumbnail;
        }
        public static async Task<VideoInformation> Select(string videoid)
        {
            try
            {
                MySqlConnection conn = new(DbSett);
                await conn.OpenAsync();
                var cmd = new MySqlCommand(
                    $"SELECT * FROM videoinformation WHERE videoid = \"{videoid}\"",
                    conn);
                var dataReader = cmd.ExecuteReader();
                var vi = new VideoInformation();
                while (await dataReader.ReadAsync())
                {
                    vi = new VideoInformation
                    {
                        VideoId = dataReader["videoid"] + "",
                        Title = dataReader["title"] + "",
                        Author = dataReader["author"] + "",
                        LengthMs = int.Parse(dataReader["length"] + ""),
                        Thumbnail = dataReader["thumbnail"] + ""
                    };
                }
                await dataReader.CloseAsync();
                await conn.CloseAsync();
                return vi;
            }
            catch (Exception)
            {
                return new VideoInformation();
            }
        }
        public static async Task Insert(VideoInformation videoInfo)
        {
            MySqlConnection conn = new(DbSett);
            await conn.OpenAsync();
            var cmd = new MySqlCommand(
                $"INSERT INTO videoinformation (videoid,title,author,length,thumbnail) VALUES (\"{videoInfo.VideoId}\", \"{videoInfo.Title.Replace("\"", "\"\"")}\", \"{videoInfo.Author.Replace("\"", "\"\"")}\", \"{videoInfo.LengthMs}\",\"{videoInfo.Thumbnail.Split("?").First()}\")",
                conn);
            cmd.ExecuteNonQuery();
            await conn.CloseAsync();
        }
    }
}