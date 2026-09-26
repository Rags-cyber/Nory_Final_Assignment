# Nory Database Setup (current approach)

This project's schema is managed **manually via SQL script**, not EF Core
migrations. `Program.cs` no longer calls `Database.Migrate()` or
`Database.EnsureCreated()` - it only checks that it can connect and that the
`AspNetRoles` table exists, then seeds the Admin/Instructor/Student roles and
a default admin account.

## Why not EF migrations?

We tried that first. Two separate bugs came up along the way:
1. The project's original baseline migration was generated for SQLite and
   never fully converted to SQL Server - string primary keys had no defined
   length, which SQL Server rejects for indexed/key columns.
2. After fixing that, a second migration generated with `Add-Migration`
   diffed against a stale snapshot and produced an invalid
   `ALTER COLUMN ... IDENTITY` statement, which SQL Server can't apply
   in-place.

Both were fixable, but for a project on a deadline, one correct SQL script
that matches the current model is simpler and more predictable than
continuing to fight migration generation.

## One-time setup

1. Open SSMS, connect to server `(localdb)\mssqllocaldb`.
2. Open `Nory_Database_Script.sql` (in the project root) and execute it
   (F5). This creates the `Nory` database and every table the app needs,
   including `Resources`, `LessonCompletions`, `StudentAwards`, and
   `ChordSongs`.
3. Run the app (F5 in Visual Studio). On startup it verifies the connection
   and schema, then seeds the roles and a default admin account:
   `admin@norymusic.com` / `Admin@123`.
4. Run `Nory_Local_Content_And_Chords.sql` in SSMS. It inserts/updates
   self-contained lesson material, theory and ear-training quizzes, and
   the Najeek guitar chord guide. It removes the old musictheory.net lesson
   links from the sample course. This script requires the app to have run
   once so the demo admin/instructor account exists.

## Real ear training (no audio files needed)

`QuizQuestion` has a `ToneSequence` column (comma-separated Hz values,
e.g. `"261.63,392.00"`). On the quiz-taking page, a question of type
`AudioIdentification` with `ToneSequence` set shows a "Play" button that
synthesizes those tones in the browser via the Web Audio API - real audio,
generated on the fly, no hosted files or licensing to worry about.
`AudioUrl` still works as before for a real uploaded/hosted audio file if
you have one; a question can use either.

If you already created your `Nory` database before this column existed,
run `Nory_Add_ToneSequence_Column.sql` once to add it (fresh installs using
the current `Nory_Database_Script.sql` already have it and don't need this).

## If you change a model (add/remove a property, add a new entity)

There is no automatic migration anymore. Update `Nory_Database_Script.sql`
to match (add the new `CREATE TABLE`/`ALTER TABLE` statements) and either:
- run just the new statements against your existing `Nory` database, or
- drop `Nory` (see below) and re-run the whole script from scratch.

For an existing database, run `Nory_Upgrade_Existing_Database.sql` in SSMS
before the local-content seed. It idempotently adds the ear-training tone
column, lesson resources, per-student lesson-completion, student awards,
and chord-song library tables. You no longer need to run the older
individual `Nory_Add_*.sql` scripts when using this combined upgrade.

## Resetting to a clean database

Run `drop-norymusic-db.bat` (Windows) or `drop-database.ps1` (PowerShell),
then re-run `Nory_Database_Script.sql`.

## Troubleshooting

- **"Could not connect to the 'Nory' database"** on startup - check the
  connection string in `appsettings.json` is
  `Server=(localdb)\mssqllocaldb;Database=Nory;...` and that you can connect
  to `(localdb)\mssqllocaldb` from SSMS.
- **"...but its tables don't exist yet"** on startup - you haven't run
  `Nory_Database_Script.sql` yet (or it was run against a different
  database/server instance). Run it, then restart the app.

## Role-specific profiles

The shared **My profile** page shows common account details plus a Student,
Instructor, and/or Admin section according to the signed-in user's assigned
role. Changes are saved in `dbo.AspNetUsers`. For an existing database, run
`Nory_Upgrade_Existing_Database.sql` to add the profile columns; a fresh
database created with `Nory_Database_Script.sql` already includes them.
