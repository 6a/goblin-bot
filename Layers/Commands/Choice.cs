namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public readonly struct Choice
    {
        public readonly string Name;
        public readonly int Value;

        public Choice(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }
}