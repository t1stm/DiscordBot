using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Methods;
using DiscordBot.Objects;
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
            var users = new List<User>();
            try
            {
                var connection = new MySqlConnection(Bot.SqlConnectionQuery);
                await connection.OpenAsync();
                var cmd = new MySqlCommand("SELECT * FROM users", connection);
                var dataReader = cmd.ExecuteReader();
                while (await dataReader.ReadAsync())
                    users.Add(new User
                    {
                        Id = (ulong) dataReader["id"],
                        Token = (dataReader["token"] ?? "") + "",
                        VerboseMessages = (bool) dataReader["verboseMessages"],
                        Language = Parser.FromNumber(ushort.Parse(dataReader["language"] + ""))
                    });
                await dataReader.CloseAsync();
                await connection.CloseAsync();
                return users;
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

        public static async Task Add(ulong authorId)
        {
            MySqlConnection connection = new(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var cmd = new MySqlCommand($"INSERT INTO users (id) VALUES (\"{authorId}\"", connection);
            cmd.ExecuteNonQuery();
            await connection.CloseAsync();
            await Controllers.Bot.LoadUsers();
        }
    }
}