using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public abstract class Command : Component
    {
        private const ulong DevUserId = 523802320627171348;
        
        private static readonly string DatabaseErrorResponse =
            $"The database shit itself, or the connection failed, or something... contact <@{DevUserId}> to let them know";
        
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
                        Log(LogSeverity.Warning, $"Failed to register command [{GetNameWithSubCommands()}]: Unable to fetch guild with ID {GuildId}");
                        return false;
                    }

                    await guild.CreateApplicationCommandAsync(slashCommand.Build());
                }

                Log(LogSeverity.Info, $"Registered {(IsGlobal ? "global" : "guild")} command [{GetNameWithSubCommands()}]");
                return true;
            }
            catch (Exception exception)
            {
                Log(LogSeverity.Warning, $"Failed to register command [{GetNameWithSubCommands()}]");
                LogException(exception);
            }
            
            return false;
        }

        public abstract Task Execute(SocketSlashCommand slashCommand);

        protected async Task LogCommandInfoAsync(SocketSlashCommand slashCommand)
        {
            var sb = new StringBuilder($"Command [{Name}] was executed by user [{GetGuildId(slashCommand)}:{GetUserId(slashCommand)}]");
            
            if (slashCommand.Data.Options is {Count: > 0})
            {
                sb.Append(" with the following options: [");
                
                AppendOptionsRecursive(sb, slashCommand.Data.Options);
            }
            
            Log(LogSeverity.Info, sb.ToString());

            await Task.CompletedTask;
        }
        
        protected static async Task SendDefaultResponseAsync(SocketSlashCommand slashCommand)
        {
            await slashCommand.RespondAsync($"The command you executed ({GetCommandName(slashCommand)}) doesn't do anything yet 😦");
        }

        protected void AddOption(Option option)
        {
            Options.Add(option);
        }
        
        protected void LogNonErrorCommandFailure(SocketSlashCommand slashCommand, string reason)
        {
            Log(LogSeverity.Verbose, $"Failed to execute command [{GetCommandName(slashCommand)}] for user [{GetGuildId(slashCommand)}:{GetUserId(slashCommand)}]: {reason}");
        }

        protected void LogCommandExecutionWithOptions(SocketSlashCommand slashCommand, params (string Name, object Value)[] options)
        {
            var sb = new StringBuilder($"Executing command [{GetCommandName(slashCommand)}] for user [{GetGuildId(slashCommand)}:{GetUserId(slashCommand)}]");

            if (options.Length > 0)
            {
                sb.Append(" with the following options:");
                foreach (var option in options)
                {
                    sb.Append($" [{option.Name}: {option.Value}]");
                }
            }
            
            Log(LogSeverity.Verbose, sb.ToString());
        }
        
        protected static SocketUser GetUser(SocketSlashCommand slashCommand)
        {
            return slashCommand.User;
        }
        
        protected static ulong GetUserId(SocketSlashCommand slashCommand)
        {
            return slashCommand.User.Id;
        }

        protected static string GetCommandName(SocketSlashCommand slashCommand)
        {
            var subCommand = slashCommand.Data.Options.FirstOrDefault();
            var subCommandString = subCommand == null ? "" : $" {subCommand.Name}";
            
            return $"{slashCommand.CommandName}{subCommandString}";
        }
        
        protected static ulong GetGuildId(SocketSlashCommand slashCommand)
        {
            return slashCommand.GuildId ?? 0;
        }
        
        protected static Dictionary<string, object> GetOptions(IEnumerable<IApplicationCommandInteractionDataOption> options)
        {
            return options.ToDictionary(option => option.Name, option => option.Options is {Count: > 0} ? option.Options : option.Value);
        }
        
        protected static async Task SendCommandNotActionedResponse(SocketSlashCommand slashCommand, string message)
        {
            await SendDefaultEmbed(
                slashCommand,
                "Your command was not able to be actioned",
                message,
                true,
                AllowedMentions.None);
        }

        protected static async Task SendDefaultDatabaseErrorResponse(SocketSlashCommand slashCommand)
        {
            await SendDefaultEmbed(
                slashCommand,
                "Something went wrong...",
                DatabaseErrorResponse,
                true,
                AllowedMentions.None);
        }
        
        protected static async Task SendDefaultEmbed(
            SocketSlashCommand slashCommand,
            string title,
            string message,
            bool isEphemeral = false,
            AllowedMentions? allowedMentions = null)
        {
            var embed = new EmbedBuilder
            {
                Title = title,
                Description = message,
                Color = Color.Green,
            };

            embed.WithFooter(new EmbedFooterBuilder
            {
                Text = "This bot is powered by the Japanese Goblins and Apes Association ⭐👺🐵⭐",
            });

            await slashCommand.RespondAsync(embed: embed.Build(), allowedMentions: allowedMentions, ephemeral: isEphemeral);
        }
        
        protected static TValue GetDefaultValueOrFallback<TKey, TValue>(IReadOnlyDictionary<TKey, object> dictionary, TKey key, TValue fallback) where TKey : notnull
        {
            var value = dictionary.GetValueOrDefault(key);
            if (value == null)
            {
                return fallback;
            }

            try
            {
                return (TValue)value;
            }
            catch (Exception)
            {
                return fallback;
            }
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