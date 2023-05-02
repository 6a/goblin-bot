namespace ChimpinOut.GoblinBot.Logging
{
    public class Logger
    {
        public Logger(DiscordSocketClient client)
        {
            client.Log += LogAsync;
        }
        
        public async Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"{MakeLogPrefix("CMD", message.Severity)} {cmdException.Command.Aliases[0]} failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
            {
                Console.WriteLine($"{MakeLogPrefix("GEN", message.Severity)} {message}");
            }

            await Task.CompletedTask;
        }

        private static string MakeLogPrefix(string logType, LogSeverity severity)
        {
            return $"[{logType[..3]}/{severity.ToString(),-8}]";
        }
    }
}