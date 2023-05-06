﻿using ChimpinOut.GoblinBot.Common;

namespace ChimpinOut.GoblinBot.Layers.Data.DbData
{
    public readonly struct DbGymLogEntry
    {
        public readonly ulong GuildId;
        public readonly ulong UserId;
        public readonly long UnixTimeStamp;
        public readonly string Nickname;
        public readonly ulong EntryNumber;

        public readonly bool IsValid;

        public DbGymLogEntry(IDataRecord dataReader)
        {
            GuildId = (ulong)dataReader.GetInt64(0);
            UserId = (ulong)dataReader.GetInt64(1);
            UnixTimeStamp = dataReader.GetInt64(2);
            Nickname = dataReader.GetString(3);
            EntryNumber = (ulong)dataReader.GetInt64(4);

            IsValid = true;
        }

        public StringBuilder AppendDisplayNameToStringBuilder(StringBuilder sb, bool format = true)
        {
            var boldMarkup = format ? "**" : null;
            var indentMarkup = format ? "> " : null;
            return sb.Append(indentMarkup).Append(boldMarkup).Append("Level ").Append(EntryNumber).Append(" - ").Append(Nickname).Append(boldMarkup);
        }

        public DateTime GetDateTime(TimeZoneInfo tzi)
        {
            return DateTimeHelper.UnixTimeToDateTime(UnixTimeStamp, tzi);
        }
        
        public bool Equals(DbGymLogEntry other)
        {
            return GuildId == other.GuildId && UserId == other.UserId && UnixTimeStamp == other.UnixTimeStamp && Nickname == other.Nickname && IsValid == other.IsValid;
        }

        public override bool Equals(object? obj)
        {
            return obj is DbGymLogEntry other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(GuildId, UserId, UnixTimeStamp, Nickname, IsValid);
        }
        
        public static bool operator ==(DbGymLogEntry lhs, DbGymLogEntry rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(DbGymLogEntry lhs, DbGymLogEntry rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}