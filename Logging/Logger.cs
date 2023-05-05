using ChimpinOut.GoblinBot.Logging.Impl;

namespace ChimpinOut.GoblinBot.Logging
{
    public class Logger
    {
        public bool IsEnabled { get; set; }
        public LogSeverity MinimumLogSeverity { get; set; }

        private readonly List<ILoggingService> _logProviders;
        
        public Logger(DiscordSocketClient client, LogSeverity minimumLogSeverity = LogSeverity.Info)
        {
            client.Log += LogAsync;
            
            IsEnabled = true;
            MinimumLogSeverity = minimumLogSeverity;

            _logProviders = new List<ILoggingService>();
        }

        public void Initialize()
        {
            _logProviders.AddRange(new ILoggingService[]
            {
                new ConsoleLoggingService(),
                new FileLoggingService(this),
            });
        }
        
        public async Task LogAsync(LogMessage message)
        {
            Log(message);
            await Task.CompletedTask;
        }

        public void Log(LogMessage message)
        {
            if (!IsEnabled || message.Severity > MinimumLogSeverity)
            {
                return;
            }
            
            if (message.Exception is CommandException cmdException)
            {
                LogImpl($"{MakeLogPrefix("Command", message.Severity)} {cmdException.Command.Aliases[0]} failed to execute in {cmdException.Context.Channel}.");
                LogImpl(cmdException.ToString());
            }
            else
            {
                LogImpl($"{MakeLogPrefix("General", message.Severity)} {message}");
            }
        }

        public void LogRaw(string message)
        {
            LogImpl(message);
        }
        
        private void LogImpl(string message)
        {
            foreach (var logProvider in _logProviders)
            {
                logProvider.Enqueue(message);
            }
        }

        private static string MakeLogPrefix(string logType, LogSeverity severity)
        {
            return $"[{ClampString(logType, 8)}/{ClampString(severity.ToString(), 8),-8}]";
        }

        private static string ClampString(string target, int maxSize)
        {
            return target[..Math.Min(maxSize, target.Length)];
        }
    }
}