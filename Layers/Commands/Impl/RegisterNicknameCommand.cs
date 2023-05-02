using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Commands.Impl
{
    public class RegisterNicknameCommand : Command
    {
        public RegisterNicknameCommand(Logger logger, DiscordSocketClient client) 
            : base(logger, client, "register-daily-nickname", "Register a nickname for the day")
        {
            Options.Add(new Option("nickname", ApplicationCommandOptionType.String, "The nickname to set for today", true));
            Options.Add(new Option("override", ApplicationCommandOptionType.Boolean, "Whether or not to override any existing nickname for today", false));
        }

        public override async Task Execute(SocketSlashCommand slashCommand)
        {
            await LogCommandInfoAsync(slashCommand);
            await SendDefaultResponseAsync(slashCommand);
        }
    }
}