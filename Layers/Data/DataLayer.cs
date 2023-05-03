using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Data
{
    public class DataLayer : Layer
    {
        private const string EnvironmentVariableSqliteDatabasePath = "SQLITE_DB_PATH_GOBLIN_BOT";
        
        protected override string LogPrefix => "Data";
        
        public DataLayer(Logger logger) : base(logger)
        {
        }

        public override async Task<bool> InitializeAsync()
        {
            await base.InitializeAsync();
            
            var sqlitePath = await GetEnvironmentVariable(EnvironmentVariableSqliteDatabasePath);
            if (string.IsNullOrWhiteSpace(sqlitePath))
            {
                return false;
            }
            
            // Attempt to connect to database, return result below
            
            return await LogAndReturnInitializationResult(true);
        }

        public async Task<DataRequestResult> GymLogAddEntry(ulong guildId, ulong userId, DateTime dateTime, string nickname, bool canOverride)
        {
            await Task.CompletedTask;
            return default;
        }
        
        public async Task<DataRequestResult> GymLogGetEntry(ulong guildId, ulong userId, DateTime dateTime)
        {
            await Task.CompletedTask;
            return default;
        }
        
        public async Task<DataRequestResult> GymLogListEntries(ulong guildId, ulong userId, uint entries)
        {
            await Task.CompletedTask;
            return default;
        }
        
        public async Task<DataRequestResult> GymLogGetStats(ulong guildId, ulong[] users)
        {
            await Task.CompletedTask;
            return default;
        }
    }
}