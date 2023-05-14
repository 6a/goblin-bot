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
            
            Log(LogSeverity.Info, $"Finished executing command [{GetCommandName(slashCommand)}] for user [[{GetGuildId(slashCommand)}:{GetUserId(slashCommand)}]");
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

            // Also trim off any whitespace, linebreak characters etc.
            var nickname = GetDefaultValueOrFallback(options, NicknameOptionText, string.Empty).Trim();
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
            
            var sb = new StringBuilder($"<@{userId}> went to the gym ");
            if (!wasSpecial)
            {
                sb.Append("on ");
            }

            sb.Append(whenString).Append(", and on ");
            sb.Append(whenString == "today" ? "this" : "that");
            sb.AppendLine(" day they were known as: ").AppendLine();
            sb = gymLogEntry.AppendNicknameToStringBuilder(sb).AppendLine();
            
            await SendDefaultEmbed(slashCommand, "Gym log updated", (await AppendStats(sb, guildId, userId)).ToString());
        }
        
        private async Task ExecuteView(SocketSlashCommand slashCommand)
        {
            var guildId = GetGuildId(slashCommand);
            
            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = GetDefaultValueOrFallback(options, UserOptionText, (SocketGuildUser)GetUser(slashCommand));

            var getUserResult = await _dataLayer.GetUser(targetUser.Id);
            if (!await ValidateUser(slashCommand, getUserResult))
            {
                return;
            }
            
            var whenString = GetDefaultValueOrFallback(options, WhenOptionText, "today");
            if (!TryParseWhenString(whenString, getUserResult.Data.Timezone, out var when, out _))
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

            var title = $"Gym log entry for {targetUser.Username} on {ToDisplayString(when)}";
            
            var sb = new StringBuilder($"On this day <@{targetUser.Id}> went to the gym, and were known as:");
            sb.AppendLine().AppendLine();
            sb = gymLogEntry.AppendNicknameToStringBuilder(sb).AppendLine();
            
            await SendDefaultEmbed(slashCommand, title, (await AppendStats(sb, guildId, targetUser.Id)).ToString());
        }

        private async Task ExecuteList(SocketSlashCommand slashCommand)
        {
            var guildId = GetGuildId(slashCommand);
            
            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = GetDefaultValueOrFallback(options, UserOptionText, (SocketGuildUser)GetUser(slashCommand));

            var getUserResult = await _dataLayer.GetUser(targetUser.Id);
            if (!await ValidateUser(slashCommand, getUserResult))
            {
                return;
            }

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

            var entriesNumberText = gymLogEntries.Count > 1 ? $"{gymLogEntries.Count} " : "";
            var entriesWordText = gymLogEntries.Count > 1 ? "entries" : "entry";
            var title = $"Showing {entriesNumberText}latest gym log {entriesWordText} for {targetUser.Username}";
            
            var sb = new StringBuilder($"Here are <@{targetUser.Id}>'s most recent gym log entries:");
            sb.AppendLine().AppendLine();
            foreach (var gymLogEntry in gymLogEntries)
            {
                sb = gymLogEntry.AppendNicknameToStringBuilder(sb, true);
                sb.Append($" ({ToDisplayString(gymLogEntry.GetDateTime(getUserResult.Data.Timezone))})").AppendLine();
            }
            
            await SendDefaultEmbed(slashCommand, title, (await AppendStats(sb, guildId, targetUser.Id)).ToString());
        }
        
        private async Task ExecuteStats(SocketSlashCommand slashCommand)
        {
            var options = GetOptions(slashCommand.Data.Options.First().Options);

            var targetUser = GetDefaultValueOrFallback(options, UserOptionText, null as SocketUser);

            if (targetUser == null)
            {
                await ExecuteStatsForServer(slashCommand);
            }
            else
            {
                await ExecuteStatsForUser(slashCommand, targetUser);
            }
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
        
        private async Task<DbGymLogStats> TryGetGymLogStats(ulong guildId, ulong userId)
        {
            var getGymLogStatsResult = await _dataLayer.GymLogGetUserStats(guildId, userId);
            if (!getGymLogStatsResult.Success)
            {
                return default;
            }

            var dbGymLogStats = getGymLogStatsResult.Data;
            if (dbGymLogStats.IsValid)
            {
                return dbGymLogStats;
            }
            
            Log(LogSeverity.Info, $"No gym log stats found for [{guildId}:{userId}]");
            return default;
        }
        
        private async Task<StringBuilder> AppendStats(StringBuilder sb, ulong guildId, ulong userId)
        {
            var dbGymLogStats = await TryGetGymLogStats(guildId, userId);
            if (!dbGymLogStats.IsValid)
            {
                return sb;
            }
            
            sb.AppendLine().Append($"They are currently level {dbGymLogStats.Level} and are ranked ");
            sb.Append(StringHelpers.ToOrdinal(dbGymLogStats.Rank)).Append(" on the server.");

            return sb;
        }

        private async Task ExecuteStatsForUser(SocketSlashCommand slashCommand, IUser user)
        {
            var guildId = GetGuildId(slashCommand);
            
            LogCommandExecutionWithOptions(slashCommand, (UserOptionText, user.Id));
                
            var title = $"Gym Log stats for {user.Username}";
                
            var dbGymLogStats = await TryGetGymLogStats(guildId, user.Id);
            if (!dbGymLogStats.IsValid)
            {
                await SendCommandNotActionedResponse(slashCommand, $"The user you specified is not currently registered with {Names.BotName} on this server, or has not logged any entries");
                return;
            }

            var sb = new StringBuilder("User is currently ");
            sb.Append(dbGymLogStats.Rank == 0 ? "unranked" : $"ranked {StringHelpers.ToOrdinal(dbGymLogStats.Rank)}");
            sb.Append(" on the server.");
                
            sb.AppendLine(" Their most recent entry is: ").AppendLine();
            dbGymLogStats.AppendNicknameToStringBuilder(sb);
            sb.AppendLine($" ({ToDisplayString(dbGymLogStats.GetDateTime(dbGymLogStats.Timezone))})");
            sb.AppendLine(StringHelpers.ZeroWidthSpace);
                
            await SendDefaultEmbed(slashCommand, title, sb.ToString());
        }
        
        private async Task ExecuteStatsForServer(SocketSlashCommand slashCommand)
        {
            var guildId = GetGuildId(slashCommand);
            
            LogCommandExecutionWithOptions(slashCommand, (UserOptionText, $"guild: {guildId}"));
               
            var title = $"Gym Log stats for the {Client.GetGuild(guildId).Name} server";
                
            var getGymLogServerStatsResult = await _dataLayer.GymLogGetServerStats(guildId);
            if (!getGymLogServerStatsResult.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }

            if (getGymLogServerStatsResult.Data.Count == 0)
            {
                LogNonErrorCommandFailure(slashCommand, "no entries found");
                await SendCommandNotActionedResponse(slashCommand, "There are no entries registered on this server yet.");
                return;
            }
            
            var sb = new StringBuilder();

            for (var statsIdx = 0; statsIdx < getGymLogServerStatsResult.Data.Count; statsIdx++)
            {
                var stats = getGymLogServerStatsResult.Data[statsIdx];
                var user = Client.GetUser(stats.UserId);
                var username = user != null ? user.Username : "UNKNOWN USER";

                sb.Append($"> **{stats.Rank}. ").Append(username).Append($" (Level {stats.Level})").AppendLine("**");
                sb.Append("> Latest entry: ");
                sb.AppendLine($"{ToDisplayString(stats.GetDateTime(stats.Timezone))} as {stats.MostRecentNickname}");

                if (statsIdx == getGymLogServerStatsResult.Data.Count - 1)
                {
                    continue;
                }
                
                sb.AppendLine(StringHelpers.ZeroWidthSpace);
            }
                
            await SendDefaultEmbed(slashCommand, title, sb.ToString());
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
    }
}