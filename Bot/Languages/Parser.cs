namespace DiscordBot.Objects
{
    public static class Parser
    {
        private static readonly English English = new();
        private static readonly Bulgarian Bulgarian = new();
        public static ILanguage FromNumber(ushort language)
        {
            return language switch
            {
                0 => English,
                1 => Bulgarian,
                _ => English
            };
        }
    }
}