namespace DiscordBot.Objects
{
    public static class Languages
    {
        public static Language FromNumber(ushort language)
        {
            return language switch
            {
                0 => Language.English,
                1 => Language.Bulgarian,
                _ => Language.English
            };
        }
        
        public enum Language
        {
            English,
            Bulgarian
        }
    }
}