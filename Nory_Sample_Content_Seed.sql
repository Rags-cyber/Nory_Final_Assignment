/*
    Deprecated legacy entry point.

    Do not use the old sample seed: it populated lesson pages with external
    musictheory.net links. The current seed is Nory_Local_Content_And_Chords.sql,
    which stores original self-contained lesson text, theory/ear-training quiz
    questions, and the chord-only song entry directly in the Nory database.

    Run Nory_Upgrade_Existing_Database.sql first when upgrading an existing DB,
    then run Nory_Local_Content_And_Chords.sql.
*/
PRINT 'Deprecated. Run Nory_Local_Content_And_Chords.sql for current sample content.';
