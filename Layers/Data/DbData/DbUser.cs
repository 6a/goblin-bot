using System.Data;
using ChimpinOut.GoblinBot.Common;

namespace ChimpinOut.GoblinBot.Layers.Data.DbData
{
    public readonly struct DbUser
    {
        public readonly ulong UserId;
        public readonly TimeZoneInfo Timezone;
        public readonly bool IsBanned;

        public readonly bool IsValid;

        public DbUser(IDataRecord dataReader)
        {
            UserId = (ulong)dataReader.GetInt64(0);
            Timezone = DateTimeHelper.TimezoneIdentifierToTimeZoneInfo(dataReader.GetString(1));
            IsBanned = dataReader.GetBoolean(2);

            IsValid = true;
        }
    }
}