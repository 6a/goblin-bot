using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Auth;
using ChimpinOut.GoblinBot.Layers.Commands;

namespace ChimpinOut.GoblinBot
{
    public static class Program
    {
        public static Task Main(string[] args) => MainAsync();

        private static DiscordSocketClient _client = null!;
        private static Logger _logger = null!;
        
        private static async Task MainAsync()
        {
            var socketConfig = new DiscordSocketConfig();
            
            _client = new DiscordSocketClient(socketConfig);
            _client.Ready += OnClientReady;

            _logger = new Logger(_client);

            var authLayer = new AuthLayer(_logger);

            await _client.LoginAsync(TokenType.Bot, await authLayer.GetBotToken());
            await _client.StartAsync();
            
            await Task.Delay(Timeout.Infinite);
        }

        private static async Task OnClientReady()
        {
            var commandLayer = new CommandLayer(_logger, _client);
            await commandLayer.RegisterCommands();
        }
    }
}