using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Commands.Impl
{
    public class DailyNicknameCommand : Command
    {
        private const string NameOptionText = "name";
        private const string OverrideOptionText = "override";

        public DailyNicknameCommand(Logger logger, DiscordSocketClient client) 
            : base(logger, client, "daily-nickname", "Set and get nicknames for today, or view previous entries")
        {
            // Set subcommand
            var setSubCommand = new Option("set", ApplicationCommandOptionType.SubCommand, "Set today's nickname");
            setSubCommand.AddSubOption(new Option(NameOptionText, ApplicationCommandOptionType.String, "Today's nickname", true));
            setSubCommand.AddSubOption(new Option(OverrideOptionText, ApplicationCommandOptionType.Boolean, "Override any existing nickname for today if one has already been registered"));
            AddOption(setSubCommand);
            
            // Get subcommand?
            //
            
            // List subcommand?
            //
        }

        public override async Task Execute(SocketSlashCommand slashCommand)
        {
            await LogCommandInfoAsync(slashCommand);
            
            switch (slashCommand.Data.Options.First().Name)
            {
                case "set": await ExecuteSet(slashCommand);
                    break;
                default:
                    await SendDefaultResponseAsync(slashCommand);
                    break;
            }
        }

        private async Task ExecuteSet(SocketSlashCommand slashCommand)
        {
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var nickname = options.GetValueOrDefault(NameOptionText) as string;
            
            options.TryGetValue(OverrideOptionText, out var shouldOverrideObject);
            var shouldOverride = shouldOverrideObject != null && (bool)shouldOverrideObject;

            await LogAsync(LogSeverity.Info, $"Executing [set] command from user [{userId}] with the following parameters: [nickname: {nickname}, override: {shouldOverride}]");

            await slashCommand.RespondAsync($"The command you executed ({Name}) is currently under development 🤓");
        }
    }
}