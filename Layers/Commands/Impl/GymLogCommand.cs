using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Data;
using ChimpinOut.GoblinBot.Layers.Data.DbData;

namespace ChimpinOut.GoblinBot.Layers.Commands.Impl
{
    public class GymLogCommand : Command
    {
        private const string RegisterCommandText = "register";
        private const string AddCommandText = "add";
        private const string ViewCommandText = "view";
        private const string ListCommandText = "list";
        private const string StatsCommandText = "stats";
        
        private const string NicknameOptionText = "nickname";
        private const string OverwriteExistingEntryOptionText = "overwrite";
        private const string UserOptionText = "user";
        private const string WhenOptionText = "when";
        private const string EntriesOptionText = "entries";

        private const string DefaultDisplayDateFormat = "YYYY/MM/DD";
        private const string DefaultStringFormatDateFormat = "yyyy/MM/dd";

        private const string DateFormatExplanation =
            $"(\"today\", \"yesterday\", or a date in the following format: {DefaultDisplayDateFormat})";

        private const long DefaultListMaxEntries = 10;
        private const int NicknameMaxLength = 40;
        
        private static readonly IFormatProvider DefaultCultureInfo = new CultureInfo("ja-JP");

        private readonly DataLayer _dataLayer;

        public GymLogCommand(Logger logger, DiscordSocketClient client, DataLayer dataLayer) 
            : base(logger, client, "gym-log", "Add or view today's gym log entry, list previous log entries, or view overall stats")
        {
            _dataLayer = dataLayer;
            
            // add subcommand
            var setSubCommand = new Option(AddCommandText, ApplicationCommandOptionType.SubCommand, "Add a log entry");
            setSubCommand.AddSubOptions(new []
            {
                new Option(NicknameOptionText, ApplicationCommandOptionType.String, "Nickname for the day", true),
                new Option(WhenOptionText, ApplicationCommandOptionType.String, $"Specify a day {DateFormatExplanation}"),
                new Option(OverwriteExistingEntryOptionText, ApplicationCommandOptionType.Boolean, "Overwrite the entry if it already exists")
            });
            AddOption(setSubCommand);
            
            // view subcommand
            var viewSubCommand = new Option(ViewCommandText, ApplicationCommandOptionType.SubCommand, "View a specific log entry");
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
                new Option(EntriesOptionText, ApplicationCommandOptionType.Integer, "Specify the maximum number of entries to list")
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
            
            Log(LogSeverity.Info, $"Finished executing command [{GetCommandName(slashCommand)}] for user [[{GetGuildId(slashCommand)}{GetUserId(slashCommand)}]");
        }

        private async Task ExecuteAdd(SocketSlashCommand slashCommand)
        {
            var guildId = GetGuildId(slashCommand);
            
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var getUserResult = await _dataLayer.GetUser(userId);
            if (!await ValidateUser(slashCommand, getUserResult))
            {
                return;
            }
            
            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var nickname = GetDefaultValueOrFallback(options, NicknameOptionText, string.Empty);
            if (nickname == string.Empty)
            {
                LogNonErrorCommandFailure(slashCommand, $"{NicknameOptionText} option was empty");
                await SendCommandNotActionedResponse(slashCommand, "You need to enter something as a nickname; it can't be empty.");
                
                return;
            }

            if (nickname.Length > NicknameMaxLength)
            {
                LogNonErrorCommandFailure(slashCommand, $"{NicknameOptionText} exceeded max length [{nickname.Length}/{NicknameMaxLength}]");
                await SendCommandNotActionedResponse(slashCommand, $"The nickname you selected is too long ({nickname.Length}/{NicknameMaxLength} characters)");
                
                return;
            }

            var whenString = GetDefaultValueOrFallback(options, WhenOptionText, "today");
            if (!TryParseWhenString(whenString, getUserResult.Data.Timezone, out var when, out var wasSpecial))
            {
                LogNonErrorCommandFailure(slashCommand, $"unable to parse when string [{whenString}]");
                await SendCommandNotActionedResponse(slashCommand, $"The \"{WhenOptionText}\" must be {DateFormatExplanation}.");
                
                return;
            }

            var shouldOverride = GetDefaultValueOrFallback(options, OverwriteExistingEntryOptionText, false);

            LogCommandExecutionWithOptions(slashCommand, (NicknameOptionText, nickname), (WhenOptionText, ToDisplayString(when)), (OverwriteExistingEntryOptionText, shouldOverride));

            var dataRequestResult = await _dataLayer.GymLogAddEntry(guildId, userId, when, nickname, shouldOverride);
            if (!dataRequestResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }
            
            var gymLogEntry = dataRequestResult.Data.AssociatedEntry;
            if (!gymLogEntry.IsValid)
            {
                LogNonErrorCommandFailure(slashCommand, $"Failed to fetch inserted/updated entry [{guildId}:{userId}:{ToDisplayString(when)}]");
                await SendDefaultDatabaseErrorResponse(slashCommand);
                
                return;
            }

            var gymLogAddEntryResult = dataRequestResult.Data;
            var associatedEntry = gymLogAddEntryResult.AssociatedEntry;
            switch (gymLogAddEntryResult.ResultCode)
            {
                case GymLogAddEntryResultCode.EntryExistsForDateSpecified:
                {
                    var duplicateDate = DateTimeHelper.UnixTimeToDateTime(associatedEntry.UnixTimeStamp, getUserResult.Data.Timezone);
                    var duplicateDateString = ToDisplayString(duplicateDate);
                    LogNonErrorCommandFailure(slashCommand, $"Entry for date [{duplicateDateString}] already exists");
                    await SendCommandNotActionedResponse(slashCommand, $"An entry already exists for \"{whenString}\" with the nickname \"{associatedEntry.Nickname}\".");
                
                    return;
                }
                case GymLogAddEntryResultCode.NicknameAlreadyUsed:
                {
                    var duplicateDate = DateTimeHelper.UnixTimeToDateTime(associatedEntry.UnixTimeStamp, getUserResult.Data.Timezone);
                    var duplicateDateString = ToDisplayString(duplicateDate);
                    LogNonErrorCommandFailure(slashCommand, $"Nickname [{nickname}] is already registered for [{duplicateDateString}]");
                    await SendCommandNotActionedResponse(slashCommand, $"An entry with the nickname \"{nickname}\" was already registered on \"{duplicateDateString}\".");
                
                    return;
                }
                case GymLogAddEntryResultCode.RecordAlreadyExists:
                {
                    var duplicateDate = DateTimeHelper.UnixTimeToDateTime(associatedEntry.UnixTimeStamp, getUserResult.Data.Timezone);
                    var duplicateDateString = ToDisplayString(duplicateDate);
                    LogNonErrorCommandFailure(slashCommand, $"An entry for date [{duplicateDateString}] with nickname [{nickname}] already exists");
                    await SendCommandNotActionedResponse(slashCommand, $"The entry you tried to add already exists (\"{nickname}\" on \"{whenString}\").");
                
                    return;
                }
            }
            
            var getGymLogStatsResult = await _dataLayer.GetGymLogStats(guildId, userId);
            if (!getGymLogStatsResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }

            var dbGymLogStats = getGymLogStatsResult.Data;
            if (!dbGymLogStats.IsValid)
            {
                LogNonErrorCommandFailure(slashCommand, "Unexpected user stats lookup error");
                await SendDefaultDatabaseErrorResponse(slashCommand);
                
                return;
            }

            var totalEntries = dbGymLogStats.Entries;
            var rank = dbGymLogStats.Rank;

            var sb = new StringBuilder($"<@{userId}> went to the gym ");
            if (!wasSpecial)
            {
                sb.Append("on ");
            }

            sb.Append(whenString).Append(", and on ");
            sb.Append(whenString == "today" ? "this" : "that");
            sb.AppendLine(" day they were known as:").AppendLine();
            sb.AppendLine($"#{gymLogEntry.EntryNumber} **{nickname}**");
            
            await SendDefaultEmbed(slashCommand, "Gym log updated", AppendStats(sb, totalEntries, rank).ToString());
        }
        
        private async Task ExecuteView(SocketSlashCommand slashCommand)
        {
            var guildId = GetGuildId(slashCommand);
            
            var user = GetUser(slashCommand);
            var userId = user.Id;
            
            var getUserResult = await _dataLayer.GetUser(userId);
            if (!await ValidateUser(slashCommand, getUserResult))
            {
                return;
            }

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = GetDefaultValueOrFallback(options, UserOptionText, (SocketGuildUser)user);

            var whenString = GetDefaultValueOrFallback(options, WhenOptionText, "today");
            if (!TryParseWhenString(whenString, getUserResult.Data.Timezone, out var when, out var wasSpecial))
            {
                LogNonErrorCommandFailure(slashCommand, $"unable to parse when string [{whenString}]");
                await SendCommandNotActionedResponse(slashCommand, $"The \"{WhenOptionText}\" must be {DateFormatExplanation}.");
                
                return;
            }

            LogCommandExecutionWithOptions(slashCommand, (UserOptionText, targetUser.Id), (WhenOptionText, ToDisplayString(when)));

            var dataRequestResult = await _dataLayer.GymLogGetEntry(guildId, targetUser.Id, when);
            if (!dataRequestResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }

            var gymLogEntry = dataRequestResult.Data;
            if (!gymLogEntry.IsValid)
            {
                LogNonErrorCommandFailure(slashCommand, $"Log Entry not found for [{guildId}:{targetUser.Id}:{ToDisplayString(when)}]");
                await SendCommandNotActionedResponse(slashCommand, $"Couldn't find an entry for <@{targetUser.Id}> on {ToDisplayString(when)}.");
                
                return;
            }
            
            var getGymLogStatsResult = await _dataLayer.GetGymLogStats(guildId, userId);
            if (!getGymLogStatsResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }

            var dbGymLogStats = getGymLogStatsResult.Data;
            if (!dbGymLogStats.IsValid)
            {
                LogNonErrorCommandFailure(slashCommand, "Unexpected user stats lookup error");
                await SendDefaultDatabaseErrorResponse(slashCommand);
                
                return;
            }

            var title = $"Gym log entry for {targetUser.Username} on {ToDisplayString(when)}";
            
            var totalEntries = dbGymLogStats.Entries;
            var rank = dbGymLogStats.Rank;

            var sb = new StringBuilder($"On this day <@{targetUser.Id}> went to the gym, and were known as:");
            sb.AppendLine().AppendLine().AppendLine($"#{gymLogEntry.EntryNumber} **{gymLogEntry.Nickname}**");
            
            await SendDefaultEmbed(slashCommand, title, AppendStats(sb, totalEntries, rank).ToString());
        }

        private async Task ExecuteList(SocketSlashCommand slashCommand)
        {
            var guildId = GetGuildId(slashCommand);
            
            var user = GetUser(slashCommand);
            var userId = user.Id;
            
            var getUserResult = await _dataLayer.GetUser(userId);
            if (!await ValidateUser(slashCommand, getUserResult))
            {
                return;
            }
            
            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = GetDefaultValueOrFallback(options, UserOptionText, user);
            var maxEntries = GetDefaultValueOrFallback(options, EntriesOptionText, DefaultListMaxEntries);

            LogCommandExecutionWithOptions(slashCommand, (UserOptionText, targetUser.Id), (EntriesOptionText, maxEntries));

            var dataRequestResult = await _dataLayer.GymLogListEntries(guildId, targetUser.Id, maxEntries);
            if (!dataRequestResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }

            var gymLogEntries = dataRequestResult.Data;
            if (gymLogEntries.Count == 0)
            {
                LogNonErrorCommandFailure(slashCommand, $"No log entries found for user [{guildId}:{targetUser.Id}]");
                await SendCommandNotActionedResponse(slashCommand, $"There are currently no entries logged for <@{targetUser.Id}>.");
                
                return;
            }
            
            var getGymLogStatsResult = await _dataLayer.GetGymLogStats(guildId, userId);
            if (!getGymLogStatsResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }

            var dbGymLogStats = getGymLogStatsResult.Data;
            if (!dbGymLogStats.IsValid)
            {
                LogNonErrorCommandFailure(slashCommand, "Unexpected user stats lookup error");
                await SendDefaultDatabaseErrorResponse(slashCommand);
                
                return;
            }
            
            var totalEntries = dbGymLogStats.Entries;
            var rank = dbGymLogStats.Rank;

            var title = $"Showing latest gym log entries for {targetUser.Username}";
            var sb = new StringBuilder($"Here are <@{targetUser.Id}>'s most recent gym log entries:");
            sb.AppendLine().AppendLine("```fix");
            for (var i = 0; i < gymLogEntries.Count; i++)
            {
                var entry = gymLogEntries[i];
                var entryNumber = totalEntries - (ulong)i;
                
                var date = DateTimeHelper.UnixTimeToDateTime(entry.UnixTimeStamp, getUserResult.Data.Timezone);
                sb.AppendLine($"#{entryNumber} [{entry.Nickname}] on {ToDisplayString(date)}");
            }

            sb.Append("```");

            await SendDefaultEmbed(slashCommand, title, AppendStats(sb, totalEntries, rank).ToString());
        }
        
        private async Task ExecuteStats(SocketSlashCommand slashCommand)
        {
            var user = GetUser(slashCommand);
            var userId = user.Id;

            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = options.GetValueOrDefault(UserOptionText) as SocketUser;
            
            Log(LogSeverity.Verbose, $"Executing [{GetCommandName(slashCommand)}] command from user [{user}] with the following parameters: [user: {targetUser}]");

            await slashCommand.RespondAsync($"The command you executed ({GetCommandName(slashCommand)}) is currently under development 🤓");
        }
        
        private async Task<bool> ValidateUser(SocketSlashCommand slashCommand, DataRequestResult<DbUser> getUserResult)
        {
            if (!getUserResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return false;
            }

            var dbUser = getUserResult.Data;
            if (!dbUser.IsValid)
            {
                LogNonErrorCommandFailure(slashCommand, "user not registered");
                await SendCommandNotActionedResponse(slashCommand, $"You need to register with the bot first using the /{RegisterCommandText} command.");

                return false;
            }

            if (dbUser.IsBanned)
            {
                LogNonErrorCommandFailure(slashCommand, "user is banned");
                await SendCommandNotActionedResponse(slashCommand, "You're banned from using this bot apparently, good job idiot.");

                return false;
            }

            return true;
        }
        
        private static bool TryParseWhenString(string whenString, TimeZoneInfo timezone, out DateTime parsedDate, out bool wasSpecial)
        {
            parsedDate = default;
            wasSpecial = false;
            var referenceDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone).Date;
            
            // Handle special cases first
            switch (whenString.ToLowerInvariant())
            {
                case "today": 
                    parsedDate = referenceDate;
                    wasSpecial = true;
                    return true;
                case "yesterday": 
                    parsedDate = referenceDate - TimeSpan.FromDays(1);
                    wasSpecial = true;
                    return true;
            }

            if (!DateTime.TryParse(whenString, DefaultCultureInfo, DateTimeStyles.None, out var parsedDateTime))
            {
                return false;
            }
            
            parsedDate = parsedDateTime.Date;
            return true;
        }

        private static string ToDisplayString(DateTime dateTime)
        {
            return dateTime.ToString(DefaultStringFormatDateFormat);
        }

        private static StringBuilder AppendStats(StringBuilder sb, ulong totalEntries, ulong rank)
        {
            sb.AppendLine().Append("They have been to the gym ");
            sb.Append($"{(totalEntries > 1 ? $"{totalEntries} times" : "once")} and are ranked ");
            sb.Append(StringHelpers.ToOrdinal(rank));
            sb.Append(" on the server.");

            return sb;
        }
    }
}