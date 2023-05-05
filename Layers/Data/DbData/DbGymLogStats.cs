using System.Data;

namespace ChimpinOut.GoblinBot.Layers.Data.DbData
{
    public readonly struct DbGymLogStats
    {
        public readonly ulong Entries;
        public readonly ulong Rank;

        public readonly bool IsValid;

        public DbGymLogStats(IDataRecord dataReader)
        {
            Entries = (ulong)dataReader.GetInt64(0);
            Rank = (ulong)dataReader.GetInt64(1);

            IsValid = true;
        }
    }
}