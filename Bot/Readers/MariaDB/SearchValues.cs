using System;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Readers.Objects;
using MySql.Data.MySqlClient;

namespace DiscordBot.Readers.MariaDB
{
    public static class SearchValues
    {
        private static async Task<string> Read(string term)
        {
            try
            {
                term = term.ToLower();
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                term = term.Replace("\'", "\'\'").Replace("\"", "\"\"").Replace("\\", "\\\\");
                var cmd = new MySqlCommand(
                    $"SELECT * FROM fuckyoutube WHERE search = \"{term}\"", connection);
                var dataReader = await cmd.ExecuteReaderAsync();
                string search = null;
                while (await dataReader.ReadAsync())
                    search = dataReader["videoid"] + "";
                await dataReader.CloseAsync();
                await connection.CloseAsync();
                return search is null or "" ? null : search;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Read error: {e}");
                return null;
            }
        }

        public static async Task<PreviousSearchResult> ReadSearchResult(string term)
        {
            try
            {
                term = term.ToLower();
                var result = new PreviousSearchResult {SearchTerm = term};
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                term = term.Replace("\"", "\"\"").Replace("\\", "\\\\");
                var cmd = new MySqlCommand(
                    $"SELECT * FROM fuckyoutube WHERE search = \"{term}\"", connection);
                var dataReader = await cmd.ExecuteReaderAsync();
                while (await dataReader.ReadAsync())
                    result.VideoId = dataReader["videoid"] + "";
                await dataReader.CloseAsync();
                await connection.CloseAsync();
                return result;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Read error: {e}");
                return null;
            }
        }

        public static async Task Add(string term, string id)
        {
            try
            {
                var read = await Read(term);
                if (!string.IsNullOrEmpty(read)) return;
                term = term.ToLower();
                MySqlConnection connection = new(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    "INSERT INTO fuckyoutube (search,videoid) " +
                    $"VALUES (\"{term}\", \"{id}\")", connection);
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