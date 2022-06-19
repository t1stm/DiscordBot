using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Methods;
using MySql.Data.MySqlClient;

namespace DiscordBot.Readers.MariaDB
{
    public static class ClientTokens
    {
        public static async Task<string> Read(ulong key)
        {
            try
            {
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    $"SELECT * FROM clienttokens WHERE id = \"{key}\"", connection);
                var dataReader = cmd.ExecuteReader();
                string token = null;
                while (await dataReader.ReadAsync())
                    token = dataReader["token"] + "";
                await dataReader.CloseAsync();
                await connection.CloseAsync();
                return token is null or "" ? null : token;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Read error: {e}");
                return "offline";
            }
        }

        public static async Task<Dictionary<ulong, string>> ReadAll()
        {
            var dict = new Dictionary<ulong, string>();

            try
            {
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM clienttokens", connection);
                var dataReader = cmd.ExecuteReader();
                while (await dataReader.ReadAsync())
                    dict.Add((ulong) dataReader["id"], dataReader["token"] + "");
                await dataReader.CloseAsync();
                await connection.CloseAsync();
                return dict;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Read error: {e}", true, Debug.DebugColor.Error);
                return null;
            }
        }

        public static async Task<bool> Add(ulong key, string value)
        {
            try
            {
                var read = await Read(key);
                if (read != null) return false;

                MySqlConnection connection = new(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    "INSERT INTO clienttokens (id,token) " +
                    $"VALUES (\"{key}\", \"{value}\")", connection);
                cmd.ExecuteNonQuery();
                await connection.CloseAsync();
                await Controllers.Bot.LoadUsers();
                return true;
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Write error: {e}", true, Debug.DebugColor.Error);
                return false;
            }
        }
    }
}