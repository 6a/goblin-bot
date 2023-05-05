CREATE TABLE "gym_log_entries" (
	"guild_id"	INTEGER NOT NULL,
	"user_id"	INTEGER NOT NULL,
	"datetime_unix"	INTEGER NOT NULL,
	"nickname"	TEXT NOT NULL,
	FOREIGN KEY("user_id") REFERENCES "users"("user_id"),
	UNIQUE("guild_id","user_id","nickname"),
	UNIQUE("guild_id","user_id","datetime_unix")
)