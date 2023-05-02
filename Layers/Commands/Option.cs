namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public readonly struct Option
    {
        public readonly string Name;
        public readonly ApplicationCommandOptionType Type;
        public readonly string Description;
        
        public readonly bool IsRequired;

        public readonly List<Option> SubOptions;
        
        public Option(string name, ApplicationCommandOptionType type, string description, bool isRequired = false)
        {
            Name = name;
            Type = type;
            Description = description;
            IsRequired = isRequired;
            SubOptions = new List<Option>();
        }

        public void AddSubOption(Option option)
        {
            SubOptions.Add(option);
        }

        public SlashCommandOptionBuilder ToSlashCommandOptionBuilder()
        {
            var command = new SlashCommandOptionBuilder
            {
                Name = Name,
                Type = Type,
                Description = Description,
                IsRequired = IsRequired,
            };
            
            for (var subOptionIdx = 0; subOptionIdx < SubOptions.Count; subOptionIdx++)
            {
                command.AddOption(SubOptions[subOptionIdx].ToSlashCommandOptionBuilder());
            }

            return command;
        }
    }
}