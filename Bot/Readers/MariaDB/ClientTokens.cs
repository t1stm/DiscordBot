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
                    $"SELECT * FROM users WHERE id = \"{key}\"", connection);
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

        public static async Task<List<User>> ReadAll()
        {
            var dict = new List<User>();

            try
            {
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM users", connection);
                var dataReader = cmd.ExecuteReader();
                while (await dataReader.ReadAsync())
                    dict.Add(new User
                    {
                        Id =  (ulong) dataReader["id"],
                        Token = (dataReader["token"] ?? "") + ""
                    });
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

        public static async Task AddToken(ulong key, string value)
        {
            try
            {
                var read = await Read(key);
                if (read != null) return;

                MySqlConnection connection = new(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand(
                    "INSERT INTO users (id,token) " +
                    $"VALUES (\"{key}\", \"{value}\")", connection);
                cmd.ExecuteNonQuery();
                await connection.CloseAsync();
                await Controllers.Bot.LoadUsers();
            }
            catch (Exception e)
            {
                await Debug.WriteAsync($"MariaDB Write error: {e}", true, Debug.DebugColor.Error);
            }
        }

        public static async Task UserSettings(ulong authorId)
        {
            var user = await Read(authorId);
            if (user is "offline" or { }) return;
            await Add(authorId);
        }

        private static async Task Add(ulong authorId)
        {
            MySqlConnection connection = new(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var cmd = new MySqlCommand($"INSERT INTO users (id) VALUES (\"{authorId}\"", connection);
            cmd.ExecuteNonQuery();
            await connection.CloseAsync();
            await Controllers.Bot.LoadUsers();
        }
    }

    public class User
    {
        public ulong Id { get; init; }
        public string Token { get; init; }
        
        
    }
}