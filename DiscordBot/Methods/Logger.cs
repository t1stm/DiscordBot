#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Methods
{
    public class LoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger();
        }
    }

    public class Logger : ILogger
    {
        public bool Enabled { get; set; } = Bot.DebugMode;

        public IDisposable BeginScope<TState>(TState state)
        {
            return default!;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => false,
                LogLevel.Debug => false,
                LogLevel.Information => true,
                LogLevel.Warning => true,
                LogLevel.Error => true,
                LogLevel.Critical => true,
                LogLevel.None => false,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!Bot.DebugMode && !Enabled) return;
            if (!IsEnabled(logLevel)) return;
            if (formatter(state, exception).ToLower().Contains("event handler")) return;
            var task = new Task(async () => await Debug.WriteAsync($"[Logger | {logLevel}] - {formatter(state, exception)}"));
            task.Start();
        }
    }
}