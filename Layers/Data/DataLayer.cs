using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Data
{
    public class DataLayer : Layer
    {
        protected override string LogPrefix => "Data";
        
        public DataLayer(Logger logger) : base(logger)
        {
        }

        public override async Task<bool> InitializeAsync()
        {

            return true;
        }

        public async Task<DataRequestResult> GymLogAddEntry(ulong guildId, ulong userId, DateTime dateTime, string nickname, bool canOverride)
        {

            return default;
        }
        
        public async Task<DataRequestResult> GymLogGetEntry(ulong guildId, ulong userId, DateTime dateTime)
        {

            return default;
        }
        
        public async Task<DataRequestResult> GymLogListEntries(ulong guildId, ulong userId, uint entries)
        {

            return default;
        }
        
        public async Task<DataRequestResult> GymLogGetStats(ulong guildId, ulong[] users)
        {

            return default;
        }
    }
}