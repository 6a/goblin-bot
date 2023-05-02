using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Common
{
    public abstract class Component
    {
        protected readonly Logger Logger;

        protected abstract string LogPrefix { get; }

        protected Component(Logger logger)
        {
            Logger = logger;
        }

        protected async Task LogAsync(LogSeverity severity, string message)
        {
            await Logger.LogAsync(new LogMessage(severity, LogPrefix, message));
        }
    }
}