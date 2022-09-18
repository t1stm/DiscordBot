using System.Threading.Tasks;
using DiscordBot.Data;
using DiscordBot.Data.Models;
using DiscordBot.Methods;
using MySql.Data.MySqlClient;

namespace DiscordBot.Objects
{
    public class User
    {
        private readonly UsersModel Model;
        public ulong Id {
            get => Model.Id;
            set => Model.Id = value;
        }
        public string Token
        {
            get => Model.Token;
            set => Model.Token = value;
        }

        public bool VerboseMessages => Model.VerboseMessages;

        public ILanguage Language
        {
            get => Parser.FromNumber(Model.Language);

            set => Model.Language = Parser.GetIndex(value);
        }

        private bool UiScroll => Model.UiScroll;
        private bool UiForceScroll => Model.ForceUiScroll;
        private bool LowSpec => Model.LowSpec;

        public User(UsersModel model)
        {
            Model = model;
        }

        public WebUISettings ToWebUISettings()
        {
            return new()
            {
                UiScroll = UiScroll,
                UiForceScroll = UiForceScroll,
                LowSpec = LowSpec
            };
        }

        public async Task ModifySettings(string target, string value)
        {
            var connection = new MySqlConnection(Bot.SqlConnectionQuery);
            await connection.OpenAsync();
            var request = $"UPDATE `users` SET `{target}` = '{value}' WHERE `users`.`id` = '{Id}'";
            if (Bot.DebugMode) await Debug.WriteAsync($"Updating user with id: ({Id}): \"{request}\"");
            var cmd = new MySqlCommand(request, connection);
            await cmd.ExecuteNonQueryAsync();
            await connection.CloseAsync();
        }

        public static async Task<User> FromId(ulong id)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching user: \"{id}\"");
            var searchUser = new UsersModel
            {
                Id = id
            };
            var selectedUser = Databases.UserDatabase.Read(searchUser);
            if (selectedUser != null)
            {
                if (Bot.DebugMode)
                    await Debug.WriteAsync(
                        $"Returning found user: \"{id}\", {selectedUser.VerboseMessages}, {selectedUser.Language}");
                return new User(selectedUser);
            }

            var newUser = new UsersModel
            {
                Id = id
            };

            Databases.UserDatabase.Add(newUser);
            return new User(newUser);
        }

        public static async Task<User?> FromToken(string token)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching user with token: \"{token}\"");
            var searchData = new UsersModel
            {
                Token = token
            };
            var select = Databases.UserDatabase.Read(searchData);
            if (select == null) return null;
            if (Bot.DebugMode)
                await Debug.WriteAsync(
                    $"Returning user with token \"{token}\": \"{select.Id}\", {select.VerboseMessages}, {select.Language}");
            return new User(select);
        }
    }

    public struct WebUISettings
    {
        public bool UiScroll { get; init; }
        public bool UiForceScroll { get; init; }
        public bool LowSpec { get; init; }
    }
}