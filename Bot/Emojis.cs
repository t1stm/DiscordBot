using System.Collections.Generic;
using DSharpPlus.Entities;

namespace Bat_Tosho
{
    public static class Emojis
    {
        public static List<DiscordEmoji> EmojiToBeUsed = new();

        public static readonly DiscordEmoji
            PlayEmoji = DiscordEmoji.FromGuildEmote(Program.Discord, 830157542491422790);

        public static bool CheckIfEmoji(string file)
        {
            EmojiToBeUsed = file.Trim() switch
            {
                "4_5793984356309535296.mp4" => new List<DiscordEmoji>
                {
                    DiscordEmoji.FromGuildEmote(Program.Discord, 832171323438006320)
                },
                "7.mp4" or "523k0jtx65a61.jpg" or "image0_301.png" or "yes_bite.mp4" or
                    "318b3ba392672ba157720a1f7b4be20ef3b4b844.mp4" or "video0_238" or "video0_407.mp4" or
                    "image0_372.jpeg" or "video0_301.mp4" or "video0_338-questionmark.mp4" or "shitpost52.mp4" or
                    "fuck_me.mp4" or "video0-106.mp4" or "video0_588.mp4" or "image0_283.png" or "iswa56xscjl21.png" or
                    "rat-cursed.mp4" => new List<DiscordEmoji> {DiscordEmoji.FromName(Program.Discord, ":question:")},
                "the_he.mp4" or "image0-2.jpg" or "image0_173.png" or "image3_19.jpg" or "image0_216.jpg" or
                    "image0_358.jpg" => new List<DiscordEmoji> {DiscordEmoji.FromName(Program.Discord, ":tired_face:")},
                "bag.mp4" => new List<DiscordEmoji>
                {
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_u:"),
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_h:"),
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_i:"),
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_t:"),
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_s:"),
                    DiscordEmoji.FromName(Program.Discord, ":telephone:"),
                    DiscordEmoji.FromName(Program.Discord, ":family_man_girl:")
                },
                "video0_443.mp4" or "j9b1bc47pn241.png" or "video0_250.mp4" or "image.jpg" or "please_mom.png" or
                    "image1.gif" or "facts-1.jpg" => new List<DiscordEmoji>
                    {
                        DiscordEmoji.FromName(Program.Discord, ":cry:")
                    },
                "im_straight.mp4" or "image1.png" or "Hifumi_Yugoslav.mp4" or "video0_710.mp4" =>
                    new List<DiscordEmoji> {DiscordEmoji.FromName(Program.Discord, ":flag_rs:")},
                "arco_mac_1331037748204101635320P_1.mp4" or "os3jol60i6a61.jpg" or
                    "62184274-63ea-4c2c-abbe-b4b65e104f1d.mp4" or "86o2nr2ob1b61.png" or "video0_464.mp4" or
                    "image0_254.png" or "video0_526.mp4" or "video0_202.mp4" or "video0_626.mp4" or "image3_31.jpg" or
                    "1i49y0lhp7g61.jpg" or "video0_227.jpg" or "video0-10.mp4" => new List<DiscordEmoji>
                    {
                        DiscordEmoji.FromName(Program.Discord, ":heart_eyes:")
                    },
                "teletubbies.mp4" => new List<DiscordEmoji>
                {
                    DiscordEmoji.FromName(Program.Discord, ":green_square:"),
                    DiscordEmoji.FromName(Program.Discord, ":red_square:"),
                    DiscordEmoji.FromName(Program.Discord, ":yellow_square:"),
                    DiscordEmoji.FromName(Program.Discord, ":blue_square:")
                },
                "ara-ara.png" or "phd.jpg" or "video0_445" or "video0_378" or "hornykeanu.mp4" or
                    "IMG-20210107-WA0081.jpg" or "video0_276.mp4" or "ara_ara_qmn40d2.jpg" or "fca7134.jpg" or
                    "chima_chima480P.mp4" or "cock.png" or "date.png" or "video0_433.mp4" or "Shitpost32.mp4" or
                    "video0_294.mp4" or "video0_274.mp4" or "weaponized_vacuum.mp4" => new List<DiscordEmoji>
                    {
                        DiscordEmoji.FromGuildEmote(Program.Discord, 807689824513425439)
                    },
                "video0_95.mp4" => new List<DiscordEmoji> {PlayEmoji},
                "prayers.mp4" or "image0_1.jpg" or "video0_319.mov" => new List<DiscordEmoji>
                {
                    DiscordEmoji.FromName(Program.Discord, ":cross:")
                },
                "1603629602444.webm" => new List<DiscordEmoji> {DiscordEmoji.FromName(Program.Discord, ":ship:")},
                "video0_216.mp4" or "video0_34.mp4" or "video0_596.mp4" => //Mickey Mouse Club
                    new List<DiscordEmoji>
                    {
                        DiscordEmoji.FromName(Program.Discord, ":regional_indicator_o:"),
                        DiscordEmoji.FromName(Program.Discord, ":regional_indicator_h:"),
                        DiscordEmoji.FromName(Program.Discord, ":regional_indicator_n:"),
                        DiscordEmoji.FromName(Program.Discord, ":regional_indicator_o:")
                    },
                "IMG_20210303_213310.jpg" or "bc66a1a.jpg" or "video0_261_new_jam.mp4" => new List<DiscordEmoji>
                {
                    DiscordEmoji.FromGuildEmote(Program.Discord, 833334153775939634),
                    DiscordEmoji.FromGuildEmote(Program.Discord, 832215075276193862)
                },
                "video0_302.mp4" or "monkeyveilcity.mp4" => new List<DiscordEmoji>
                {
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_y:"),
                    DiscordEmoji.FromName(Program.Discord, ":regional_indicator_a:"),
                    DiscordEmoji.FromGuildEmote(Program.Discord, 833337593299075102)
                },
                _ => new List<DiscordEmoji>()
            };
            return EmojiToBeUsed.Count >= 1;
        }
    }
}