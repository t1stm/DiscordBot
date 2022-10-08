using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Methods
{
    public static class Debug
    {
        public enum DebugColor
        {
            Standard,
            Warning,
            Error,
            Urgent
        }

        public const string DebugTimeDateFormat = "dd/MM/yyyy hh:mm:ss tt";

        public static async Task WriteAsync(string text, bool save = false, DebugColor color = DebugColor.Standard)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            switch (color)
            {
                case DebugColor.Standard:
                    break;
                case DebugColor.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case DebugColor.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DebugColor.Urgent:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(color), color,
                        "Somehow this is out of range and I don't have a single clue how.");
            }

            var date = $"{DateTime.Now.ToString(DebugTimeDateFormat)}";
            var transformed = $"{date}: {text}";
            Console.WriteLine(transformed);
            if (!save) return;
            await File.AppendAllTextAsync($"{Bot.WorkingDirectory}/BatTosho_latest.log", transformed + "\n",
                Encoding.UTF8);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static void Write(string text, bool save = false, DebugColor color = DebugColor.Standard)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            switch (color)
            {
                case DebugColor.Standard:
                    break;
                case DebugColor.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case DebugColor.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DebugColor.Urgent:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(color), color,
                        "Somehow this is out of range and I don't have a single clue how.");
            }

            var date = $"{DateTime.Now.ToString(DebugTimeDateFormat)}";
            var transformed = $"{date}: {text}";
            Console.WriteLine(transformed);
            if (!save) return;
            File.AppendAllText($"{Bot.WorkingDirectory}/BatTosho_latest.log", transformed + "\n", Encoding.UTF8);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}