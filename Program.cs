using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Auth;
using ChimpinOut.GoblinBot.Layers.Commands;
using ChimpinOut.GoblinBot.Layers.Data;

namespace ChimpinOut.GoblinBot
{
    public static class Program
    {
        public static Task Main(string[] args) => MainAsync();

        private static DiscordSocketClient? _client;
        private static Logger? _logger;

        private static AuthLayer? _authLayer;
        private static DataLayer? _dataLayer;

        private static bool _isReady;
        
        private static async Task MainAsync()
        {
            var socketConfig = new DiscordSocketConfig();
            
            _client = new DiscordSocketClient(socketConfig);
            _client.Ready += OnClientReady;

            _logger = new Logger(_client, LogSeverity.Verbose);

            _authLayer = new AuthLayer(_logger);
            if (!await _authLayer.InitializeAsync())
            {
                await NotifyAndShutdownOnInitFailure();
                return;
            }
            
            _dataLayer = new DataLayer(_logger);
            if (!await _dataLayer.InitializeAsync())
            {
                await NotifyAndShutdownOnInitFailure();
                return;
            }

            await _client.LoginAsync(TokenType.Bot, _authLayer.BotToken);
            await _client.StartAsync();

            while (!_isReady)
            {
                await Task.Delay(100);
            }
            
            var commandLayer = new CommandLayer(_logger, _client, _dataLayer);
            if (!await commandLayer.InitializeAsync())
            {
                await NotifyAndShutdownOnInitFailure();
                return;
            }
            
            await Task.Delay(Timeout.Infinite);
        }

        private static Task OnClientReady()
        {
            _isReady = true;
            return Task.CompletedTask;
        }

        private static async Task NotifyAndShutdownOnInitFailure()
        {
            if (_logger != null)
            {
                var logMessage = new LogMessage(LogSeverity.Critical, "Server", "Failed to initialize the server. Shutting down...");
                await _logger.LogAsync(logMessage);

                _logger.IsEnabled = false;
            }

            if (_client != null)
            {
                await _client.StopAsync();
            }
        }
    }
}