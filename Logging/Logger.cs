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
                Console.WriteLine($"{MakeLogPrefix("Command", message.Severity)} {cmdException.Command.Aliases[0]} failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
            {
                Console.WriteLine($"{MakeLogPrefix("General", message.Severity)} {message}");
            }

            await Task.CompletedTask;
        }

        private static string MakeLogPrefix(string logType, LogSeverity severity)
        {
            return $"[{ClampString(logType, 8)}/{ClampString(severity.ToString(), 8),-8}]";
        }

        private static string ClampString(string target, int maxSize)
        {
            return target[..Math.Min(8, target.Length)];
        }
    }
}