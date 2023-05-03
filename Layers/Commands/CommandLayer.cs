using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Commands.Impl;
using ChimpinOut.GoblinBot.Layers.Data;

namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public class CommandLayer : Layer
    {
        protected override string LogPrefix => "Command";
        
        private readonly DiscordSocketClient _client;

        private readonly Dictionary<string, Command> _commands;

        private readonly DataLayer _dataLayer;

        public CommandLayer(Logger logger, DiscordSocketClient client, DataLayer dataLayer) : base(logger)
        {
            _client = client;
            _dataLayer = dataLayer;
            _commands = new Dictionary<string, Command>();
        }

        public override async Task<bool> InitializeAsync()
        {
            await base.InitializeAsync();
            
            if (!await RegisterCommand(new GymLogCommand(Logger, _client)))
            {
                return false;
            }
            
            // Other commands go here
            //
            
            _client.SlashCommandExecuted += HandleSlashCommandExecuted;
            
            return await LogAndReturnInitializationResult(true);
        }
        
        private async Task<bool> RegisterCommand(Command command)
        {
            if (_commands.ContainsKey(command.Name))
            {
                await LogAsync(LogSeverity.Error, $"Slash command {command.Name} is already registered");
                return false;
            }
            
            if (!await command.Register())
            {
                return false;
            }

            _commands.Add(command.Name, command);
            return true;
        }
        
        private async Task HandleSlashCommandExecuted(SocketSlashCommand slashCommand)
        {
            var commandName = slashCommand.Data.Name;
            if (!_commands.TryGetValue(commandName, out var command))
            {
                await LogAsync(LogSeverity.Error, $"Slash command {commandName} is not registered");
                await slashCommand.RespondAsync($"The command you executed ({commandName}) was not found 😖");
                return;
            }

            await command.Execute(slashCommand);
        }
    }
}