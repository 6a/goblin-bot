namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public readonly struct Option
    {
        public readonly string Name;
        public readonly ApplicationCommandOptionType Type;
        public readonly string Description;
        
        public readonly bool IsRequired;

        public readonly List<Option> SubOptions;
        
        public readonly List<Choice> Choices;
        
        public Option(string name, ApplicationCommandOptionType type, string description, bool isRequired = false)
        {
            Name = name;
            Type = type;
            Description = description;
            IsRequired = isRequired;
            
            SubOptions = new List<Option>();
            
            Choices = new List<Choice>();
        }

        public Option AddSubOptions(IEnumerable<Option> options)
        {
            foreach (var option in options)
            {
                SubOptions.Add(option);
            }

            return this;
        }

        public Option AddChoices(IEnumerable<Choice> choices)
        {
            foreach (var choice in choices)
            {
                Choices.Add(choice);
            }
            
            return this;
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

            foreach (var option in SubOptions)
            {
                command.AddOption(option.ToSlashCommandOptionBuilder());
            }

            foreach (var choice in Choices)
            {
                switch (Type)
                {
                    case ApplicationCommandOptionType.String:
                        command.AddChoice(choice.Name, choice.GetValue<string>());
                        break;
                    case ApplicationCommandOptionType.Integer:
                        command.AddChoice(choice.Name, choice.GetValue<long>());
                        break;
                    case ApplicationCommandOptionType.Number:
                        command.AddChoice(choice.Name, choice.GetValue<double>());
                        break;
                }
            }

            return command;
        }
    }
}