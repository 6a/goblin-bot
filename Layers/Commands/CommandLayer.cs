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
            await RegisterCommand(new RegisterNicknameCommand(Logger, _client));
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
            if (!_commands.TryGetValue(slashCommand.Data.Name, out var command))
            {
                return;
            }

            await command.Execute(slashCommand);
        }
    }
}