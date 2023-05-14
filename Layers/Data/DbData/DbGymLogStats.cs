using ChimpinOut.GoblinBot.Common;

namespace ChimpinOut.GoblinBot.Layers.Data.DbData
{
    public readonly struct DbGymLogStats
    {
        public readonly ulong UserId;
        public readonly string MostRecentNickname;
        public readonly long MostRecentNicknameUnixTimeStamp;
        public readonly TimeZoneInfo Timezone;
        public readonly ulong Level;
        public readonly ulong Rank;

        public readonly bool IsValid;

        public DbGymLogStats(IDataRecord dataReader)
        {
            UserId = (ulong)dataReader.GetInt64(0);
            MostRecentNickname = dataReader.GetString(1);
            MostRecentNicknameUnixTimeStamp = dataReader.GetInt64(2);
            Timezone = DateTimeHelper.TimezoneIdentifierToTimeZoneInfo(dataReader.GetString(3));
            Level = (ulong)dataReader.GetInt64(4);
            Rank = (ulong)dataReader.GetInt64(5);

            IsValid = true;
        }
        
        public StringBuilder AppendNicknameToStringBuilder(StringBuilder sb, bool format = true)
        {
            return StringHelpers.AppendNicknameToStringBuilder(sb, Level, MostRecentNickname, format);
        }

        public DateTime GetDateTime(TimeZoneInfo tzi)
        {
            return DateTimeHelper.UnixTimeToDateTime(MostRecentNicknameUnixTimeStamp, tzi);
        }
    }
}