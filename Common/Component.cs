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

        protected void Log(LogSeverity severity, string message)
        {
            Logger.Log(new LogMessage(severity, LogPrefix, message));
        }
        
        protected void LogException(Exception exception)
        {
            Logger.LogRaw(exception.ToString());
        }
    }
}