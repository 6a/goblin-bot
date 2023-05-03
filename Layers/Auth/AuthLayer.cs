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
            BotToken = await GetBotToken();
            return BotToken != string.Empty;
        }
        
        private async Task<string> GetBotToken()
        {
            try
            {
                var token = Environment.GetEnvironmentVariable(EnvironmentVariableBotToken);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    await LogAsync(LogSeverity.Info, $"Bot token environment variable ({EnvironmentVariableBotToken}) read successfully");
                    return token;
                }
                
                await LogAsync(LogSeverity.Critical, $"Bot token environment variable ({EnvironmentVariableBotToken}) was null or empty");
                return string.Empty;
            }
            catch (SecurityException securityException)
            {
                await LogAsync(LogSeverity.Critical, $"Failed to read bot token environment variable ({EnvironmentVariableBotToken}): {securityException}");
                return string.Empty; 
            }
        }
    }
}