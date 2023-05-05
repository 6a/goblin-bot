namespace ChimpinOut.GoblinBot.Logging.Impl
{
    public readonly struct TimedLogMessage
    {
        public readonly bool IsValid;
        
        public readonly DateTime Timestamp;
        private readonly string _message;

        public TimedLogMessage(DateTime timestamp, string message)
        {
            Timestamp = timestamp;
            _message = message;

            IsValid = true;
        }

        public override string ToString()
        {
            return $"[{Timestamp:u}] {_message}";
        }
    }
}