using ChimpinOut.GoblinBot.Layers.Data.DbData;

namespace ChimpinOut.GoblinBot.Layers.Data
{
    public readonly struct GymLogAddEntryResult
    {
        public readonly GymLogAddEntryResultCode ResultCode;
        public readonly DbGymLogEntry AssociatedEntry;

        public GymLogAddEntryResult(GymLogAddEntryResultCode resultCode, DbGymLogEntry associatedEntry)
        {
            ResultCode = resultCode;
            AssociatedEntry = associatedEntry;
        }
    }
}