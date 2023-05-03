using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Commands.Impl
{
    public class GymLogCommand : Command
    {
        private const string AddCommandText = "add";
        private const string ViewCommandText = "view";
        private const string ListCommandText = "list";
        private const string StatsCommandText = "stats";
        
        private const string NickNameOptionText = "nickname";
        private const string OverrideOptionText = "override";
        private const string UserOptionText = "user";
        private const string WhenOptionText = "when";
        private const string EntriesOptionText = "entries";

        public GymLogCommand(Logger logger, DiscordSocketClient client) 
            : base(logger, client, "gym-log", "Add or view today's gym log entry, list previous log entries, or view overall stats")
        {
            // add subcommand
            var setSubCommand = new Option(AddCommandText, ApplicationCommandOptionType.SubCommand, "Add a log entry for today");
            setSubCommand.AddSubOptions(new []
            {
                new Option(NickNameOptionText, ApplicationCommandOptionType.String, "Today's nickname", true),
                new Option(OverrideOptionText, ApplicationCommandOptionType.Boolean, "Override today's entry if it has already been logged")
            });
            AddOption(setSubCommand);
            
            // view subcommand
            var viewSubCommand = new Option(ViewCommandText, ApplicationCommandOptionType.SubCommand, "View today's log entry");
            viewSubCommand.AddSubOptions(new []
            {
                new Option(UserOptionText, ApplicationCommandOptionType.User, "Specify a user to view the entry for a user other than yourself"),
                new Option(WhenOptionText, ApplicationCommandOptionType.String, "Specify a date to view the entry for a day that isn't today")
            });
            AddOption(viewSubCommand);
            
            // list subcommand
            var listSubCommand = new Option(ListCommandText, ApplicationCommandOptionType.SubCommand, "List previous log entries");
            listSubCommand.AddSubOptions(new []
            {
                new Option(UserOptionText, ApplicationCommandOptionType.User, "Specify a user to list the entries for a user other than yourself"),
                new Option(EntriesOptionText, ApplicationCommandOptionType.String, "Specify the maximum number of entries to list")
            });
            AddOption(listSubCommand);
            
            // stats subcommand
            var statsSubCommand = new Option(StatsCommandText, ApplicationCommandOptionType.SubCommand, "View overall stats");
            statsSubCommand.AddSubOptions(new []
            {
                new Option(UserOptionText, ApplicationCommandOptionType.User, "Specify a user to list the stats for a specific user, rather than the whole server")
            });
            AddOption(statsSubCommand);
        }

        public override async Task Execute(SocketSlashCommand slashCommand)
        {
            await LogCommandInfoAsync(slashCommand);
            
            switch (slashCommand.Data.Options.First().Name)
            {
                case AddCommandText: await ExecuteAdd(slashCommand);
                    break;
                case ViewCommandText: await ExecuteView(slashCommand);
                    break;
                case ListCommandText: await ExecuteList(slashCommand);
                    break;
                case StatsCommandText: await ExecuteStats(slashCommand);
                    break;
                default:
                    await SendDefaultResponseAsync(slashCommand);
                    break;
            }
        }

        private async Task ExecuteAdd(SocketSlashCommand slashCommand)
        {
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var nickname = options.GetValueOrDefault(NickNameOptionText) as string;
            
            options.TryGetValue(OverrideOptionText, out var shouldOverrideObject);
            var shouldOverride = shouldOverrideObject != null && (bool)shouldOverrideObject;

            await LogAsync(LogSeverity.Verbose, $"Executing [{GetDisplayName(slashCommand)}] command from user [{user}] with the following parameters: [nickname: {nickname}, override: {shouldOverride}]");

            await slashCommand.RespondAsync($"The command you executed ({GetDisplayName(slashCommand)}) is currently under development 🤓");
        }
        
        private async Task ExecuteView(SocketSlashCommand slashCommand)
        {
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = options.GetValueOrDefault(UserOptionText) as SocketUser;
            targetUser ??= user;

            var whenString = options.GetValueOrDefault(WhenOptionText) as string;
            whenString ??= "all";
            
            await LogAsync(LogSeverity.Verbose, $"Executing [{GetDisplayName(slashCommand)}] command from user [{user}] with the following parameters: [user: {targetUser}, when: {whenString}]");

            await slashCommand.RespondAsync($"The command you executed ({GetDisplayName(slashCommand)}) is currently under development 🤓");
        }
        
        private async Task ExecuteList(SocketSlashCommand slashCommand)
        {
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = options.GetValueOrDefault(UserOptionText) as SocketUser;
            targetUser ??= user;
            
            var entriesString = options.GetValueOrDefault(EntriesOptionText) as string;
            entriesString ??= "5";
            
            await LogAsync(LogSeverity.Verbose, $"Executing [{GetDisplayName(slashCommand)}] command from user [{user}] with the following parameters: [user: {targetUser}, entries: {entriesString}]");

            await slashCommand.RespondAsync($"The command you executed ({GetDisplayName(slashCommand)}) is currently under development 🤓");
        }
        
        private async Task ExecuteStats(SocketSlashCommand slashCommand)
        {
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = options.GetValueOrDefault(UserOptionText) as SocketUser;
            
            await LogAsync(LogSeverity.Verbose, $"Executing [{GetDisplayName(slashCommand)}] command from user [{user}] with the following parameters: [user: {targetUser}]");

            await slashCommand.RespondAsync($"The command you executed ({GetDisplayName(slashCommand)}) is currently under development 🤓");
        }
    }
}