using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public abstract class Command : Component
    {
        protected override string LogPrefix => "Command";
        
        public string Name { get; init; }
        public string Description { get; init; }
        
        public ulong GuildId { get; init; }
        public bool IsGlobal { get; init; }

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
                        await LogAsync(LogSeverity.Warning, $"Failed to register command \"{Name}\": Unable to fetch guild with ID {GuildId}");
                        return false;
                    }

                    await guild.CreateApplicationCommandAsync(slashCommand.Build());
                }

                await LogAsync(LogSeverity.Info, $"Registered {(IsGlobal ? "global" : "guild")} command \"{Name}\"");
                return true;
            }
            catch (HttpException exception)
            {
                await LogAsync(LogSeverity.Warning, $"Failed to register command \"{Name}\": {exception.Message}");
            }
            catch (ArgumentNullException exception)
            {
                await LogAsync(LogSeverity.Warning, $"Failed to register command \"{Name}\": {exception.Message}");
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
            var sb = new StringBuilder($"Command \"{Name}\" was executed by user [{slashCommand.User.Username}]");
            
            if (slashCommand.Data.Options is {Count: > 0})
            {
                sb.Append(" with the following options: [");
                foreach (var option in slashCommand.Data.Options)
                {
                    sb.Append($"{option.Name}:{option.Value}").Append(", ");
                }

                sb.Remove(sb.Length - 2, 2).Append(']');
            }
            
            await LogAsync(LogSeverity.Info, sb.ToString());
        }
        
        protected async Task SendDefaultResponseAsync(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync($"The command you executed ({Name}) doesn't do anything yet 😦");
        }
    }
}