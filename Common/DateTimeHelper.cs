namespace ChimpinOut.GoblinBot.Common
{
    // Reference: https://jp.cybozu.help/general/en/admin/list_systemadmin/list_localization/timezone.html
    public static class DateTimeHelper
    {
        private static readonly DateTime Epoch = new (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        
        public static readonly ReadOnlyDictionary<string, string> TimeZoneDisplayNameToIdMap = new(new Dictionary<string, string>
        {
            {"(UTC+09:00) Osaka, Sapporo, Tokyo", "Asia/Tokyo"},
            {"(UTC+00:00) Dublin, Edinburgh, Lisbon, London", "Europe/London"},
            {"(UTC+01:00) Brussels, Copenhagen, Madrid, Paris", "Europe/Paris"},
        });
        
        public static readonly ReadOnlyDictionary<string, string> TimeZoneIdToDisplayNameMap = new(
            TimeZoneDisplayNameToIdMap.ToDictionary(x => x.Value, x => x.Key));

        public static readonly ReadOnlyCollection<string> AllTimeZoneDisplayNames = 
            TimeZoneDisplayNameToIdMap.Keys.OrderBy(ToTimezoneDouble).ToList().AsReadOnly();
        
        public static readonly ReadOnlyCollection<string> AllTimeZoneIdentifiers = 
            TimeZoneDisplayNameToIdMap.Values.ToList().AsReadOnly();

        public static TimeZoneInfo TimezoneIdentifierToTimeZoneInfo(string identifier)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(identifier);
            }
            catch (Exception)
            {
                // If we couldn't parse it there isn't much else we can do so just use UTC...
                return TimeZoneInfo.Utc;
            }
        }
                
        public static DateTime UnixTimeToDateTime(long unixTime, TimeZoneInfo timeZoneInfo)
        {
            return ConvertDateTimeWithTimezone(Epoch.AddSeconds(unixTime).ToUniversalTime(), timeZoneInfo);
        }

        public static DateTime ConvertDateTimeWithTimezone(DateTime dateTime, TimeZoneInfo timeZoneInfo)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo);
        }

        private static double ToTimezoneDouble(string timezoneDisplayName)
        {
            return double.Parse(timezoneDisplayName[4..10].Replace(':', '.'));
        }
    }
}