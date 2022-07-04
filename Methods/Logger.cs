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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            /*if (!IsEnabled(logLevel)) return;
            var task = new Task(async () => await Debug.WriteAsync($"[DSharpPlus] - {formatter(state, exception)}"));
            task.Start();*/
        }
    }
}