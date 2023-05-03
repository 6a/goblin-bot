using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Auth
{
    public class AuthLayer : Layer
    {
        private const string EnvironmentVariableBotToken = "DISCORD_BOT_TOKEN_GOBLIN_BOT";
     
        public string BotToken { get; private set; }
        
        protected override string LogPrefix => "Auth";
        
        public AuthLayer(Logger logger) : base(logger)
        {
            BotToken = string.Empty;
        }

        public override async Task<bool> InitializeAsync()
        {
            await base.InitializeAsync();
            BotToken = await GetEnvironmentVariable(EnvironmentVariableBotToken);

            return await LogAndReturnInitializationResult(BotToken != string.Empty);
        }
    }
}