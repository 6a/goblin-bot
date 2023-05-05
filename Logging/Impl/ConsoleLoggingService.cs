namespace ChimpinOut.GoblinBot.Logging.Impl
{
    public class ConsoleLoggingService : ILoggingService
    {
        public void Enqueue(string message)
        {
            Console.WriteLine(message);
        }
    }
}