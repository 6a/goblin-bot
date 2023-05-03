using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public abstract class Command : Component
    {
        protected override string LogPrefix => "Command";
        
        public string Name { get; }
        public string Description { get; }
        
        public ulong GuildId { get; }
        public bool IsGlobal { get; }

        protected readonly DiscordSocketClient Client;

        protected readonly List<Option> Options;
        
        protected Command(Logger logger, DiscordSocketClient client, string name, string description) : base(logger)
        {
            Name = name;
            Description = description;

            Client = client;

            GuildId = 0;
            IsGlobal = true;

            Options = new List<Option>();
        }

        public async Task<bool> Register()
        {
            try
            {
                var slashCommand = new SlashCommandBuilder
                {
                    Name = Name,
                    Description = Description,
                };

                for (var optionIdx = 0; optionIdx < Options.Count; optionIdx++)
                {
                    slashCommand.AddOption(Options[optionIdx].ToSlashCommandOptionBuilder());
                }
                
                if (IsGlobal)
                {
                    await Client.CreateGlobalApplicationCommandAsync(slashCommand.Build());
                }
                else
                {
                    var guild = Client.GetGuild(GuildId);
                    if (guild == null)
                    {
                        await LogAsync(LogSeverity.Warning, $"Failed to register command [{GetNameWithSubCommands()}]: Unable to fetch guild with ID {GuildId}");
                        return false;
                    }

                    await guild.CreateApplicationCommandAsync(slashCommand.Build());
                }

                await LogAsync(LogSeverity.Info, $"Registered {(IsGlobal ? "global" : "guild")} command [{GetNameWithSubCommands()}]");
                return true;
            }
            catch (HttpException exception)
            {
                await LogAsync(LogSeverity.Warning, $"Failed to register command [{GetNameWithSubCommands()}]: {exception.Message}");
            }
            catch (ArgumentNullException exception)
            {
                await LogAsync(LogSeverity.Warning, $"Failed to register command [{GetNameWithSubCommands()}]: {exception.Message}");
            }
            catch (Exception exception)
            {
                await LogAsync(LogSeverity.Warning, $"Unhandled exception occurred while attempting to register command \"{Name}\": {exception.Message}");
            }
            
            return false;
        }

        public abstract Task Execute(SocketSlashCommand slashCommand);

        protected async Task LogCommandInfoAsync(SocketSlashCommand slashCommand)
        {
            var sb = new StringBuilder($"Command [{Name}] was executed by user [{GetUser(slashCommand)}]");
            
            if (slashCommand.Data.Options is {Count: > 0})
            {
                sb.Append(" with the following options: [");
                
                AppendOptionsRecursive(sb, slashCommand.Data.Options);
            }
            
            await LogAsync(LogSeverity.Info, sb.ToString());
        }
        
        protected static async Task SendDefaultResponseAsync(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync($"The command you executed ({GetDisplayName(slashCommand)}) doesn't do anything yet 😦");
        }

        protected void AddOption(Option option)
        {
            Options.Add(option);
        }

        protected static SocketUser GetUser(SocketSlashCommand slashCommand)
        {
            return slashCommand.User;
        }

        protected static string GetDisplayName(SocketSlashCommand slashCommand)
        {
            var subCommand = slashCommand.Data.Options.FirstOrDefault();
            var subCommandString = subCommand == null ? "" : $" {subCommand.Name}";
            
            return $"{slashCommand.CommandName}{subCommandString}";
        }
        
        protected static Dictionary<string, object> GetOptions(IEnumerable<IApplicationCommandInteractionDataOption> options)
        {
            return options.ToDictionary(option => option.Name, option => option.Options is {Count: > 0} ? option.Options : option.Value);
        }

        private static void AppendOptionsRecursive(StringBuilder sb, IEnumerable<IApplicationCommandInteractionDataOption> options)
        {
            var hasRecursed = false;
            foreach (var option in options)
            {
                sb.Append(option.Name).Append(": ");
                if (option.Options == null || option.Options.Count == 0)
                {
                    sb.Append(option.Value).Append(", ");
                }
                else
                {
                    sb.Append('[');
                    AppendOptionsRecursive(sb, option.Options);

                    hasRecursed = true;
                }
            }

            if (!hasRecursed)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            
            sb.Append(']');
        }

        private string GetNameWithSubCommands()
        {
            var sb = new StringBuilder(Name);

            foreach (var option in Options.Where(option => option.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup))
            {
                sb.Append($" <{option.Name}>");
            }
            
            return sb.ToString();
        }
    }
}