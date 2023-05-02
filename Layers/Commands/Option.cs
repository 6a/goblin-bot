namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public readonly struct Option
    {
        public readonly string Name;
        public readonly ApplicationCommandOptionType Type;
        public readonly string Description;
        
        public readonly bool IsRequired;

        public Option(string name, ApplicationCommandOptionType type, string description, bool isRequired)
        {
            Name = name;
            Type = type;
            Description = description;
            IsRequired = isRequired;
        }

        public SlashCommandOptionBuilder ToSlashCommandOptionBuilder()
        {
            return new SlashCommandOptionBuilder
            {
                Name = Name,
                Type = Type,
                Description = Description,
                IsRequired = IsRequired,
            };
        }
    }
}