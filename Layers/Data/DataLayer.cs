using ChimpinOut.GoblinBot.Logging;
using ChimpinOut.GoblinBot.Layers.Data.DbData;

namespace ChimpinOut.GoblinBot.Layers.Data
{
    public class DataLayer : Layer
    {
        private const string EnvironmentVariableSqliteDatabasePath = "SQLITE_DB_PATH_GOBLIN_BOT";
        private const int DefaultCommandTimeout = 5;
        
        protected override string LogPrefix => "Data";

        private string _connectionString;
        
        public DataLayer(Logger logger) : base(logger)
        {
            _connectionString = string.Empty;
        }

        public override async Task<bool> InitializeAsync()
        {
            await base.InitializeAsync();
            
            var sqlitePath = await GetEnvironmentVariable(EnvironmentVariableSqliteDatabasePath);
            if (string.IsNullOrWhiteSpace(sqlitePath))
            {
                return false;
            }
            
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = sqlitePath,
                Mode = SqliteOpenMode.ReadWrite,
                DefaultTimeout = DefaultCommandTimeout,
            }.ToString();

            // Test the connection
            var connectionTestSucceeded = false;
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                }
                
                connectionTestSucceeded = true;
                Log(LogSeverity.Info, "Database connection verified");
            }
            catch (Exception exception)
            {
                Log(LogSeverity.Error, "Unable to verify the connection to the database");
                LogException(exception);
            }

            return LogAndReturnInitializationResult(connectionTestSucceeded);
        }

        public async Task<DataRequestResult<RegisterUserResultCode>> RegisterDbUser(ulong userId, string timezoneIdentifier)
        {
            var userRequestResult = await GetUser(userId);
            if (!userRequestResult.Success)
            {
                return default;
            }

            if (userRequestResult.Data.IsBanned)
            {
                return new DataRequestResult<RegisterUserResultCode>(true, RegisterUserResultCode.UserIsBanned);
            }

            var willUpdate = userRequestResult.Data.IsValid;
            
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        INSERT OR REPLACE INTO users (user_id, timezone) VALUES ({userId}, $tzi)
                    ";
                    
                    // the timezone is a string, and though discord should only allow the predefined options through,
                    // Alex might try to find a way to break it so add it via the helper, just in case
                    command.Parameters.AddWithValue("$tzi", timezoneIdentifier);

                    command.ExecuteNonQuery();
                    
                    var resultCode = willUpdate ? RegisterUserResultCode.UpdatedExistingUser : RegisterUserResultCode.AddedNewUser;
                    return new DataRequestResult<RegisterUserResultCode>(true, resultCode);
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, $"Database error when attempting to fetch user {userId}");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<DbUser>> GetUser(ulong userId)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        SELECT * FROM users WHERE user_id = {userId}
                    ";

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            return new DataRequestResult<DbUser>(true, default);
                        }

                        await reader.ReadAsync();
                        
                        return new DataRequestResult<DbUser>(true, new DbUser(reader));
                    }
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, $"Database error when attempting to fetch user {userId}");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<DbGymLogEntry>> GetMostRecentEntry(ulong guildId, ulong userId)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        SELECT * from gym_log_entries
                        WHERE guild_id = {guildId} AND user_id = {userId}
                        ORDER BY entry_number DESC LIMIT 1
                    ";

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            return new DataRequestResult<DbGymLogEntry>(true, default);
                        }

                        await reader.ReadAsync();
                        
                        return new DataRequestResult<DbGymLogEntry>(true, new DbGymLogEntry(reader));
                    }
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, $"Database error when attempting to fetch entry for user [{guildId}:{userId}]S");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<GymLogAddEntryResult>> GymLogAddEntry(ulong guildId, ulong userId, DateTime dateTime, string nickname, bool shouldOverwrite)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // check for a date collision
                    var command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        SELECT * FROM gym_log_entries 
                        WHERE guild_id = {guildId} AND user_id = {userId} AND datetime_unix = {ToUnixTime(dateTime)}
                    ";

                    DbGymLogEntry dateCollision = default;
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            dateCollision = new DbGymLogEntry(reader);
                        }
                    }

                    // Date already has an entry and overwrite is not specified
                    if (!shouldOverwrite && dateCollision.IsValid)
                    {
                        var result = new GymLogAddEntryResult(GymLogAddEntryResultCode.EntryExistsForDateSpecified, dateCollision);
                        return new DataRequestResult<GymLogAddEntryResult>(true, result);
                    }
                    
                    // check for a name collision
                    command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        SELECT * FROM gym_log_entries 
                        WHERE guild_id = {guildId} AND user_id = {userId} AND nickname = $nickname
                    ";
                    
                    // Add nickname via the helper to avoid injection attacks (in case Alex tries something)
                    command.Parameters.AddWithValue("$nickname", nickname);

                    DbGymLogEntry nicknameCollision = default;
                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();
                            nicknameCollision = new DbGymLogEntry(reader);
                        }
                    }
                    
                    // Name is already in use somewhere and overwrite is not specified
                    if (!shouldOverwrite && nicknameCollision.IsValid)
                    {
                        var result = new GymLogAddEntryResult(GymLogAddEntryResultCode.NicknameAlreadyUsed, nicknameCollision);
                        return new DataRequestResult<GymLogAddEntryResult>(true, result);
                    }

                    // Date doesn't have an entry BUT nickname is already in use
                    if (!dateCollision.IsValid && nicknameCollision.IsValid)
                    {
                        var result = new GymLogAddEntryResult(GymLogAddEntryResultCode.NicknameAlreadyUsed, nicknameCollision);
                        return new DataRequestResult<GymLogAddEntryResult>(true, result);
                    }

                    // Date already has an entry AND nickname is already in use BUT they are used in two different entries
                    if (dateCollision.IsValid && nicknameCollision.IsValid && dateCollision != nicknameCollision)
                    {
                        var result = new GymLogAddEntryResult(GymLogAddEntryResultCode.NicknameAlreadyUsed, nicknameCollision);
                        return new DataRequestResult<GymLogAddEntryResult>(true, result);
                    }
                    
                    // Date already has an entry AND nickname is already in use AND they both correspond to the same entry
                    if (dateCollision.IsValid && nicknameCollision.IsValid && dateCollision == nicknameCollision)
                    {
                        var result = new GymLogAddEntryResult(GymLogAddEntryResultCode.RecordAlreadyExists, dateCollision);
                        return new DataRequestResult<GymLogAddEntryResult>(true, result);
                    }

                    var dateTimeUnix = ToUnixTime(dateTime);
                    
                    command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        INSERT INTO gym_log_entries (guild_id, user_id, datetime_unix, nickname)
                        VALUES({guildId}, {userId}, {dateTimeUnix}, $nickname)
                        ON CONFLICT DO UPDATE SET
                            datetime_unix = {dateTimeUnix},
	                        nickname = $nickname
                        WHERE guild_id = {guildId} AND user_id = {userId}
                    ";
                    
                    // Add nickname via the helper to avoid injection attacks (in case Alex tries something)
                    command.Parameters.AddWithValue("$nickname", nickname);

                    command.ExecuteNonQuery();

                    var dataRequestResult = await GymLogGetEntry(guildId, userId, dateTime);
                    if (!dataRequestResult.Success)
                    {
                        // Output should be handled in the above function, 
                        return default;
                    }

                    var associatedEntry = dataRequestResult.Data;
                    if (!associatedEntry.IsValid)
                    {
                        // Output should be handled by the caller, 
                        var entryInvalidResult = new GymLogAddEntryResult(GymLogAddEntryResultCode.Success, associatedEntry);
                        return new DataRequestResult<GymLogAddEntryResult>(true, entryInvalidResult);
                    }

                    var successResult = new GymLogAddEntryResult(GymLogAddEntryResultCode.Success, associatedEntry);
                    return new DataRequestResult<GymLogAddEntryResult>(true, successResult);
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, "Failed to add gym log entry");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<DbGymLogEntry>> GymLogGetEntry(ulong guildId, ulong userId, DateTime dateTime)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText =
                    $@"
                        SELECT * FROM gym_log_entries 
                        WHERE guild_id = {guildId} AND user_id = {userId} AND datetime_unix = {ToUnixTime(dateTime)}
                    ";

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            return new DataRequestResult<DbGymLogEntry>(true, default);
                        }

                        await reader.ReadAsync();
                        
                        return new DataRequestResult<DbGymLogEntry>(true, new DbGymLogEntry(reader));
                    }
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, "Failed to fetch gym log entry");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<List<DbGymLogEntry>>> GymLogListEntries(ulong guildId, ulong userId, long maxEntries)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText =
                    $@"
                        SELECT * FROM 
                        (
                          SELECT *, RANK () OVER (ORDER BY datetime_unix ASC) level
                          FROM gym_log_entries
                          WHERE guild_id = {guildId} AND user_id = {userId}
                        )
                        ORDER BY datetime_unix DESC LIMIT {maxEntries}
                    ";

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<DbGymLogEntry>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new DbGymLogEntry(reader));
                        }

                        await reader.ReadAsync();
                        
                        return new DataRequestResult<List<DbGymLogEntry>>(true, results);
                    }
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, "Failed to list gym entries");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<DbGymLogStats>> GymLogGetUserStats(ulong guildId, ulong userId)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        SELECT 
                            gym_log_entries.user_id,
                            nickname AS most_recent_nickname, 
                            datetime_unix AS most_recent_nickname_timestamp, 
                            timezone, 
                            entries AS level, 
                            rank FROM
                        (
                            SELECT *, RANK () OVER (ORDER BY entries DESC) rank
                            FROM gym_log_stats
                            WHERE guild_id = {guildId} AND user_id = {userId}
                        ) ranks
                        INNER JOIN gym_log_entries
                        ON gym_log_entries.nickname =
                        (
                            SELECT nickname FROM gym_log_entries
                            WHERE guild_id = ranks.guild_id AND user_id = ranks.user_id
                            ORDER BY datetime_unix DESC LIMIT 1
                        )
                        INNER JOIN users
                        ON users.user_id = ranks.user_id
                        WHERE ranks.guild_id = gym_log_entries.guild_id AND ranks.user_id = gym_log_entries.user_id
                    ";

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            return new DataRequestResult<DbGymLogStats>(true, default);
                        }

                        await reader.ReadAsync();
                        
                        return new DataRequestResult<DbGymLogStats>(true, new DbGymLogStats(reader));
                    }
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, "Failed to get user stats");
                LogException(exception);

                return default;
            }
        }
        
        public async Task<DataRequestResult<List<DbGymLogStats>>> GymLogGetServerStats(ulong guildId)
        {
            try
            {
                await using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = connection.CreateCommand();
                    command.CommandText = 
                    $@"
                        SELECT
                            gym_log_entries.user_id,
                            nickname AS most_recent_nickname,
                            datetime_unix AS most_recent_nickname_timestamp,
                            timezone,
                            entries AS level,
                            rank FROM
                        (
                          SELECT *, RANK () OVER (ORDER BY entries DESC) rank
                          FROM gym_log_stats
                        ) ranks
                        INNER JOIN gym_log_entries
                        ON gym_log_entries.nickname =
                        (
                          SELECT nickname FROM gym_log_entries
                          WHERE guild_id = {guildId} AND user_id = ranks.user_id
                          ORDER BY datetime_unix DESC LIMIT 1
                        )
                        INNER JOIN users
                        ON users.user_id = ranks.user_id
                        LIMIT 20
                    ";

                    await using (var reader = await command.ExecuteReaderAsync())
                    {
                        var results = new List<DbGymLogStats>();

                        while (await reader.ReadAsync())
                        {
                            results.Add(new DbGymLogStats(reader));
                        }

                        await reader.ReadAsync();
                        
                        return new DataRequestResult<List<DbGymLogStats>>(true, results);
                    }
                }
            }
            catch (SqliteException exception)
            {
                Log(LogSeverity.Error, "Failed to get server stats");
                LogException(exception);

                return default;
            }
        }

        private static long ToUnixTime(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }
    }
}