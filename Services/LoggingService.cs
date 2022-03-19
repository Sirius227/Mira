using Discord;

namespace Mira.Services
{
    public static class LoggingService
    {
        public static async Task LogAsync(string src, LogSeverity severity, string message, Exception exception = null!)
        {
            if (severity.Equals(null))
            {
                severity = LogSeverity.Warning;
            }
            await Append($"{GetSeverityString(severity)}", GetConsoleColor(severity));
            await Append($" [{SourceToString(src)}] ", ConsoleColor.Green);

            if (!string.IsNullOrWhiteSpace(message))
            {
                await Append($"{message}\n", ConsoleColor.Blue);
            }

            else if (exception == null)
            {
                await Append("Exception is null\n", ConsoleColor.DarkRed);
            }

            else if (exception.Message == null)
                await Append($"Not know \n{exception.StackTrace}\n", GetConsoleColor(severity));

            else
                await Append($"{exception.Message ?? "Message Unknown"}\n{exception.StackTrace ?? "Stack Trace Unknown"}\n", GetConsoleColor(severity));
        }

        public static async Task LogCriticalAsync(string source, string message, Exception exc = null!)
            => await LogAsync(source, LogSeverity.Critical, message, exc);

        public static async Task LogInformationAsync(string source, string message)
            => await LogAsync(source, LogSeverity.Info, message);

        private static async Task Append(string message, ConsoleColor color)
        {
            await Task.Run(() => {
                Console.ForegroundColor = color;
                Console.Write(message);
            });
        }

        private static string SourceToString(string src)
        {
            if (src == null)
                return null!;

            return (src.ToLower()) switch
            {
                "discord" => "DISCORD",
                "victoria" => "VICTORIA",
                "audio" => "AUDIO",
                "admin" => "ADMIN",
                "gateway" => "GATEWAY",
                "blacklist" => "BLACKLIST",
                "lavanode_0_socket" => "LAVASOCKET",
                "lavanode_0" => "LAVANODE",
                "bot" => "BOT",
                _ => src,
            };
        }

        private static string GetSeverityString(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => "CRIT",
                LogSeverity.Debug => "DEBUG",
                LogSeverity.Error => "ERROR",
                LogSeverity.Info => "INFO",
                LogSeverity.Verbose => "VERBOSE",
                LogSeverity.Warning => "WARNING",
                _ => "UNKNOWN",
            };
        }

        private static ConsoleColor GetConsoleColor(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => ConsoleColor.Red,
                LogSeverity.Debug => ConsoleColor.Magenta,
                LogSeverity.Error => ConsoleColor.DarkRed,
                LogSeverity.Info => ConsoleColor.Green,
                LogSeverity.Verbose => ConsoleColor.DarkCyan,
                LogSeverity.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White,
            };
        }
    }
}
