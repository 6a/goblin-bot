CREATE TRIGGER "update_gym_log_entries_entry_number"
  AFTER INSERT
    ON "gym_log_entries"
      BEGIN
        UPDATE gym_log_entries 
        SET entry_number = 
        (
          SELECT COUNT()
          FROM gym_log_entries
          WHERE guild_id = NEW.guild_id AND user_id = NEW.user_id
        )
        WHERE guild_id = NEW.guild_id AND user_id = NEW.user_id AND datetime_unix = NEW.datetime_unix;
      END