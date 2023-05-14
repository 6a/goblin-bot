using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Data;

namespace ChimpinOut.GoblinBot.Layers.Commands.Impl
{
    public class RegisterUserCommand : Command
    {
        private const string TimeZoneOptionText = "timezone";
        private const string CockSizeOptionText = "cock-size";
        
        private readonly DataLayer _dataLayer;
        
        public RegisterUserCommand(Logger logger, DiscordSocketClient client, DataLayer dataLayer) 
            : base(logger, client, "register", "Register as a user, or update your details if you're already registered")
        {
            _dataLayer = dataLayer;
            
            // add subcommand
            var timezoneOption = new Option(TimeZoneOptionText, ApplicationCommandOptionType.String, "Your timezone", true);
            foreach (var timezone in DateTimeHelper.TimeZoneDisplayNameToIdMap)
            {
                timezoneOption.Choices.Add(new Choice(timezone.Key, timezone.Value));
            }
            AddOption(timezoneOption);
            
            var cockSizeOption = new Option(CockSizeOptionText, ApplicationCommandOptionType.Number, "the length if your cock (in inches)");
            AddOption(cockSizeOption);
        }

        public override async Task Execute(SocketSlashCommand slashCommand)
        {
            await LogCommandInfoAsync(slashCommand);
            
            var user = GetUser(slashCommand);
            var userId = user.Id;
            
            var options = GetOptions(slashCommand.Data.Options);

            var timeZoneIdentifier = GetDefaultValueOrFallback(options, TimeZoneOptionText, string.Empty);
            if (timeZoneIdentifier == string.Empty)
            {
                LogNonErrorCommandFailure(slashCommand, $"{TimeZoneOptionText} option was empty");
                await SendCommandNotActionedResponse(slashCommand, "You need to enter a timezone; it can't be empty");
                
                return;
            }

            // This shouldn't happen, but just in case...
            if (!DateTimeHelper.TimeZoneIdToDisplayNameMap.TryGetValue(timeZoneIdentifier, out var timeZoneDisplayName))
            {
                LogNonErrorCommandFailure(slashCommand, $"{TimeZoneOptionText} was invalid");
                await SendCommandNotActionedResponse(slashCommand, "You somehow managed to enter a timezone that wasn't included in the options!");

                return;
            }
            
            var cockLength = GetDefaultValueOrFallback(options, TimeZoneOptionText, double.MinValue);
            if (cockLength > double.MinValue)
            {
                LogNonErrorCommandFailure(slashCommand, "User tried to register the size of their appendage");
                await SendCommandNotActionedResponse(slashCommand, "Are you serious? You think I'm really going to record that?");
                
                return;
            }
            
            var result = await _dataLayer.RegisterDbUser(userId, timeZoneIdentifier);
            if (!result.Success)
            {
                await SendDefaultDatabaseErrorResponse(slashCommand);
                return;
            }
            
            if (result.Data == RegisterUserResultCode.UserIsBanned)
            {
                LogNonErrorCommandFailure(slashCommand, "user is banned");
                await SendCommandNotActionedResponse(slashCommand, "You're banned from using this bot apparently, good job idiot");
                
                return;
            }

            var title = result.Data switch
            {
                RegisterUserResultCode.AddedNewUser => "Successfully registered with the following info",
                RegisterUserResultCode.UpdatedExistingUser => "Successfully updated your info",
                _ => string.Empty
            };

            await SendDefaultEmbed(slashCommand, title, $" • {TimeZoneOptionText}: {timeZoneDisplayName}", true);
            Log(LogSeverity.Info, $"Finished executing command [{GetCommandName(slashCommand)}] for user [{GetUserId(slashCommand)}]");
        }
    }
}