namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public readonly struct Choice
    {
        public readonly string Name;
        public readonly object Value;

        public Choice(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public TChoice? GetValue<TChoice>()
        {
            try
            {
                return (TChoice)Value;
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}