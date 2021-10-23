using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using DSharpPlus.Entities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Bat_Tosho.Methods
{
    public class ServerSettings
    {
        public bool GenerateStatusbarButtons { get; set; } = false;
        public bool UpdateMessagePosition { get; set; } = true;

        public ServerSettings(DiscordGuild guild)
        {
            string directory = $"{Program.MainDirectory}ServerSettings.json";
            var json = JsonSerializer.Deserialize<Dictionary<ulong, ServerSettings>>(File.ReadAllText(directory));
            if (json != null && !json.ContainsKey(guild.Id))
            {
                GenerateStatusbarButtons = json[guild.Id].GenerateStatusbarButtons;
                UpdateMessagePosition = json[guild.Id].UpdateMessagePosition;
            }
            else
            {
                json?.Add(guild.Id, new ServerSettings(UpdateMessagePosition, GenerateStatusbarButtons));
                File.WriteAllText(directory, JsonSerializer.Serialize(json));
            }
        }
        [JsonConstructor]
        public ServerSettings(bool updateMessagePosition, bool generateStatusbarButtons)
        {
            UpdateMessagePosition = updateMessagePosition;
            GenerateStatusbarButtons = generateStatusbarButtons;
        }
        public void Modify(DiscordGuild guild, bool? messagePos = null, bool? statButtons = null)
        {
            string directory = $"{Program.MainDirectory}ServerSettings.json";
            messagePos ??= UpdateMessagePosition;
            statButtons ??= GenerateStatusbarButtons;
            GenerateStatusbarButtons = statButtons.Value;
            UpdateMessagePosition = messagePos.Value;
            var text = File.ReadAllText(directory);
            var json = JsonSerializer.Deserialize<Dictionary<ulong, ServerSettings>>(text);
            if (json == null) return;
            json[guild.Id].UpdateMessagePosition = UpdateMessagePosition;
            json[guild.Id].GenerateStatusbarButtons = GenerateStatusbarButtons;
            json.Add(guild.Id, this);
            File.WriteAllText(directory, JsonSerializer.Serialize(json));
        }
    }
}