using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Commands.Impl;

namespace ChimpinOut.GoblinBot.Layers.Commands
{
    public class CommandLayer : Layer
    {
        protected override string LogPrefix => "Command";
        
        private readonly DiscordSocketClient _client;

        private readonly Dictionary<string, Command> _commands;

        public CommandLayer(Logger logger, DiscordSocketClient client) : base(logger)
        {
            _client = client;
            _client.SlashCommandExecuted += HandleSlashCommandExecuted;

            _commands = new Dictionary<string, Command>();
        }

        public async Task RegisterCommands()
        {
            await RegisterCommand(new GymLogCommand(Logger, _client));
        }
        
        private async Task RegisterCommand(Command command)
        {
            if (!await command.Register())
            {
                return;
            }
            
            _commands.TryAdd(command.Name, command);
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