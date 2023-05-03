-- Updates the gym_log_stats db each time a row is inserted into the gym_log_entries db
-- I couldn't figure out how to do and IF ELSE statement so the logic is...
-- 1. Attempt to insert a row into gym_log_stats with the same guild and user id as the updated rows in gym_log_entries
--   * If a row with the same guild and user id already exists in gym_log_stats, the error is ignored
-- 2. Update the row in gym_log_stats with the same guild and user id as the updated row, by incrementing the entries column by one

CREATE TRIGGER "update_gym_log_stats"
  AFTER INSERT
    ON "gym_log_entries"
      BEGIN
	    INSERT OR IGNORE INTO gym_log_stats(guild_id, user_id, entries) VALUES(NEW.guild_id, NEW.user_id, 0);
	    UPDATE gym_log_stats SET entries = entries + 1 WHERE guild_id = NEW.guild_id AND user_id = NEW.user_id;
      END