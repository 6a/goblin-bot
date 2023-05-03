CREATE TABLE "gym_log_stats" (
	"guild_id"	INTEGER NOT NULL,
	"user_id"	INTEGER NOT NULL,
	"entries"	INTEGER NOT NULL,
	UNIQUE("user_id","guild_id")
)