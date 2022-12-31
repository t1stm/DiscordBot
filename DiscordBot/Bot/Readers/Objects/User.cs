#nullable enable
using System.Threading.Tasks;
using DiscordBot.Data;
using DiscordBot.Data.Models;
using DiscordBot.Methods;

namespace DiscordBot.Objects
{
    public class User
    {
        private readonly UsersModel Model;

        public User(UsersModel model)
        {
            Model = model;
        }

        public ulong Id
        {
            get => Model.Id;
            set
            {
                Model.Id = value;
                Model.SetModified?.Invoke();
            }
        }

        public string? Token
        {
            get => Model.Token;
            set
            {
                Model.Token = value;
                Model.SetModified?.Invoke();
            }
        }

        public bool VerboseMessages
        {
            get => Model.VerboseMessages;
            set
            {
                Model.VerboseMessages = value;
                Model.SetModified?.Invoke();
            }
        }

        public ILanguage Language
        {
            get => Parser.FromNumber(Model.Language);

            set
            {
                Model.Language = Parser.GetIndex(value);
                Model.SetModified?.Invoke();
            }
        }

        private bool UiScroll
        {
            get => Model.UiScroll;
            set
            {
                Model.UiScroll = value;
                Model.SetModified?.Invoke();
            }
        }

        private bool UiForceScroll
        {
            get => Model.ForceUiScroll;
            set
            {
                Model.ForceUiScroll = value;
                Model.SetModified?.Invoke();
            }
        }

        private bool LowSpec
        {
            get => Model.LowSpec;
            set
            {
                Model.LowSpec = value;
                Model.SetModified?.Invoke();
            }
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

        public static async Task<User> FromId(ulong id)
        {
            if (Bot.DebugMode) await Debug.WriteAsync($"Searching user: \"{id}\"");
            var searchModel = new UsersModel
            {
                Id = id
            };
            var selectedUser = Databases.UserDatabase.Read(searchModel);
            if (selectedUser == null) return new User(Databases.UserDatabase.Add(searchModel));
            if (Bot.DebugMode)
                await Debug.WriteAsync(
                    $"Returning found user: \"{id}\", {selectedUser.VerboseMessages}, {selectedUser.Language}");
            return new User(selectedUser);
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