/*
    Nory local-first learning content and song chord library seed.
    Run after Nory_Database_Script.sql / Nory_Upgrade_Existing_Database.sql.
    Idempotent: updates the demo course, removes its old third-party lesson
    links, and inserts missing quiz/chord records without duplicating them.

    Lesson explanations below are original instructional summaries, not
    copied page text. Song entry contains chord-only guidance (no lyrics/tabs).
*/
USE [Nory];
GO

DECLARE @InstructorId NVARCHAR(450) =
    (SELECT TOP (1) Id FROM dbo.AspNetUsers WHERE Email = N'instructor@norymusic.com');
IF @InstructorId IS NULL
    SET @InstructorId = (SELECT TOP (1) Id FROM dbo.AspNetUsers WHERE Email = N'admin@norymusic.com');
IF @InstructorId IS NULL
    THROW 51001, 'Run the app once so the demo instructor/admin user exists, or create the demo course first.', 1;

DECLARE @CourseId INT =
    (SELECT TOP (1) Id FROM dbo.Courses WHERE Title = N'Music Theory Fundamentals');
IF @CourseId IS NULL
BEGIN
    INSERT dbo.Courses (Title, Description, InstructorId, StartDate, EndDate, IsActive, ImageUrl, CreatedAt)
    VALUES (N'Music Theory Fundamentals',
        N'A self-contained starter course in notation, rhythm, scales, intervals, and chords, with in-app theory and listening practice.',
        @InstructorId, CONVERT(date, SYSUTCDATETIME()), DATEADD(month, 3, SYSUTCDATETIME()), 1, NULL, SYSUTCDATETIME());
    SET @CourseId = SCOPE_IDENTITY();
END
ELSE
    UPDATE dbo.Courses
    SET Description = N'A self-contained starter course in notation, rhythm, scales, intervals, and chords, with in-app theory and listening practice.',
        InstructorId = @InstructorId
    WHERE Id = @CourseId;

-- Make the supplied demo learner ready to try the sample course immediately.
DECLARE @DemoStudentId NVARCHAR(450) =
    (SELECT TOP (1) Id FROM dbo.AspNetUsers WHERE Email = N'student@norymusic.com');
IF @DemoStudentId IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @DemoStudentId AND CourseId = @CourseId AND Status = 2)
        UPDATE dbo.Enrollments SET Status = 0 WHERE StudentId = @DemoStudentId AND CourseId = @CourseId;
    ELSE IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE StudentId = @DemoStudentId AND CourseId = @CourseId)
        INSERT dbo.Enrollments (StudentId, CourseId, EnrolledAt, Status, ProgressPercentage)
        VALUES (@DemoStudentId, @CourseId, SYSUTCDATETIME(), 0, 0);
END

DECLARE @Lesson1 INT, @Lesson2 INT, @Lesson3 INT, @Lesson4 INT, @Lesson5 INT;

-- Migrate the earlier sample lesson labels so existing enrollments and lesson
-- records are retained rather than creating a second set of sample lessons.
UPDATE dbo.Lessons SET Title = N'Intervals and Ear Training'
WHERE CourseId = @CourseId AND Title = N'Intervals';
UPDATE dbo.Lessons SET Title = N'Triads and Basic Chords'
WHERE CourseId = @CourseId AND Title = N'Introduction to Chords';

MERGE dbo.Lessons AS target
USING (VALUES
 (1, N'The Staff, Clefs, and Note Duration',
  N'Music is written on a five-line staff. A note placed higher on the staff represents a higher letter-name pitch. A clef assigns pitch names to the lines: the treble clef is commonly used for higher parts, while the bass clef covers lower parts. Notes can extend above or below the staff on short ledger lines. Rhythm is shown by note values: a whole note lasts twice as long as a half note, a half note twice as long as a quarter note, and a quarter note twice as long as an eighth note. A dot adds half of a note value to itself; for example, a dotted half note lasts three quarter-note beats. Rests use corresponding symbols to show silence. Count steadily and identify both pitch position and note value before playing.'),
 (2, N'Steps, Accidentals, and Key Signatures',
  N'The distance between neighboring keys on a piano is a half step; two half steps make a whole step. A sharp raises a note by one half step and a flat lowers it by one half step. A natural sign cancels an earlier sharp or flat for the remainder of that measure. Key signatures place sharps or flats after the clef and apply them throughout the piece unless canceled. Sharps appear in the order F-C-G-D-A-E-B; flats follow the reverse order B-E-A-D-G-C-F. To identify a major key with sharps, the final sharp is one half step below the tonic. For flat keys, the next-to-last flat names the major key (with one flat, F major is the exception).'),
 (3, N'The Major Scale',
  N'A major scale contains seven different letter names and returns to its starting note at the octave. Its interval pattern is whole, whole, half, whole, whole, whole, half (W-W-H-W-W-W-H). Starting on C gives C-D-E-F-G-A-B-C. Starting elsewhere may require sharps or flats so that the pattern remains correct and each letter name appears once. Scale degrees are numbered from the tonic: 1 is tonic, 4 is subdominant, 5 is dominant, and 7 is the leading tone. Practice by saying the note names, then singing or playing the scale while listening for the half steps between degrees 3-4 and 7-1.'),
 (4, N'Intervals and Ear Training',
  N'An interval describes the distance between two pitches. Count letter names inclusively to find its generic number: C to G spans C-D-E-F-G, so it is a fifth. The specific quality (such as major, minor, or perfect) depends on the number of semitones. A perfect fifth spans seven semitones; a major third spans four; a minor third spans three. For ear training, listen first to the lower pitch, then the higher pitch. Hum the two notes, compare their spacing, and only then choose or type an interval name. For note-identification practice, replay a single tone and match it to a familiar reference pitch. The in-app player synthesizes tones in the browser, so these exercises do not require an external audio site.'),
 (5, N'Triads and Basic Chords',
  N'A triad is built by stacking every other note of a scale: root, third, and fifth. A major triad has a major third and perfect fifth (four then three semitones); a minor triad has a minor third and perfect fifth (three then four). A diminished triad uses two minor thirds, while an augmented triad uses two major thirds. Inversions change which chord tone is lowest without changing the chord identity: root position has the root in the bass, first inversion the third, and second inversion the fifth. On guitar, chord diagrams show which strings and frets produce the chord tones. Move between shapes slowly, keep unused strings quiet, and check that each note rings clearly.')
) AS source(OrderIndex, Title, Content)
ON target.CourseId = @CourseId AND target.Title = source.Title
WHEN MATCHED THEN UPDATE SET target.Content = source.Content, target.OrderIndex = source.OrderIndex
WHEN NOT MATCHED THEN INSERT (Title, Content, CourseId, OrderIndex, CreatedAt)
    VALUES (source.Title, source.Content, @CourseId, source.OrderIndex, SYSUTCDATETIME());

-- Expanded, structured lesson notes summarized from the supplied, openly
-- licensed textbook. The Razor page renders ## headings, - objectives,
-- Example:, and Practice: lines as instructional components.
UPDATE dbo.Lessons
SET Content =
    N'## Learning goals' + CHAR(10) +
    N'- Connect written note position with pitch and octave.' + CHAR(10) +
    N'- Read staff lines, spaces, clefs, and ledger lines.' + CHAR(10) +
    N'- Explain how accidentals change a written pitch.' + CHAR(10) +
    N'## Pitch and notation' + CHAR(10) +
    N'Pitch describes how high or low a sound is. A staff has five lines and four spaces; moving one step from a line to the next space advances one letter name. A clef assigns those positions to pitches: treble is common for higher parts and bass for lower parts. Notes outside the staff use short ledger lines. Octave numbers distinguish registers; middle C is C4 in scientific pitch naming.' + CHAR(10) +
    N'## Accidentals and note spelling' + CHAR(10) +
    N'A sharp raises a note by one semitone, a flat lowers it by one semitone, and a natural cancels an earlier alteration. Double sharps and flats shift by two semitones. Enharmonic spellings sound alike on a fixed-pitch keyboard but their written names depend on musical context.' + CHAR(10) +
    N'## Rhythm preview' + CHAR(10) +
    N'In common time, a whole note represents four quarter-note beats; half, quarter, eighth, and sixteenth notes divide that duration by two at each step. Rests represent silence of corresponding durations. A dot adds half the note''s value; a tie joins durations of the same pitch.' + CHAR(10) +
    N'Example: A dotted half note equals three quarter-note beats: two beats plus one added beat.' + CHAR(10) +
    N'Practice: Name the treble-staff lines from bottom to top, then identify middle C on its ledger line.' + CHAR(10) +
    N'Source: Hutchinson, chapters 1 and 4, pp. 1-4 and 19-28.'
WHERE CourseId = @CourseId AND Title = N'The Staff, Clefs, and Note Duration';

UPDATE dbo.Lessons
SET Content =
    N'## Learning goals' + CHAR(10) +
    N'- Distinguish semitones from whole tones.' + CHAR(10) +
    N'- Construct and check a major scale.' + CHAR(10) +
    N'- Read and identify major key signatures.' + CHAR(10) +
    N'## Semitones and whole tones' + CHAR(10) +
    N'On a piano, a semitone is the distance to the nearest neighboring key; two semitones make a whole tone. Some adjacent white keys have no black key between them, so count the actual semitone distances rather than assuming every letter-to-letter move is the same size.' + CHAR(10) +
    N'## The major-scale pattern' + CHAR(10) +
    N'A major scale follows W-W-H-W-W-W-H. You can hear it as two matching four-note tetrachords joined by a whole tone. Preserve the letter sequence without skipping or repeating letter names; add sharps or flats where needed to keep the interval pattern correct.' + CHAR(10) +
    N'Example: C major is C-D-E-F-G-A-B-C. The half steps occur from E to F and B to C.' + CHAR(10) +
    N'## Key signatures' + CHAR(10) +
    N'A key signature groups the sharps or flats used by a key at the start of each staff line. Sharps appear in the order F-C-G-D-A-E-B; flats reverse that order, B-E-A-D-G-C-F. For a sharp key, move up one semitone from its final sharp to find the major tonic. For a flat key with at least two flats, the penultimate flat names the major key; one flat is F major.' + CHAR(10) +
    N'Practice: Write the W-W-H-W-W-W-H pattern from G. Check that the scale contains each letter once.' + CHAR(10) +
    N'Source: Hutchinson, chapter 2, pp. 5-10.'
WHERE CourseId = @CourseId AND Title = N'Steps, Accidentals, and Key Signatures';

UPDATE dbo.Lessons
SET Content =
    N'## Learning goals' + CHAR(10) +
    N'- Build a major scale using its interval pattern.' + CHAR(10) +
    N'- Compare natural, harmonic, and melodic minor.' + CHAR(10) +
    N'- Name scale degrees by number and function.' + CHAR(10) +
    N'## Major scale review' + CHAR(10) +
    N'The major pattern is W-W-H-W-W-W-H. Scale-degree numbers start at 1 on the tonic and continue through 7 before returning to the octave. These degrees create a map for melodies and chords, not just a sequence of notes.' + CHAR(10) +
    N'## Three minor-scale forms' + CHAR(10) +
    N'Natural minor uses the pattern W-H-W-W-H-W-W. Harmonic minor raises scale degree 7 compared with natural minor, creating a stronger pull toward the tonic. In the classical melodic-minor form, degrees 6 and 7 are raised while ascending; the descending form returns to natural minor. Hear each form against the same tonic to notice its distinct color.' + CHAR(10) +
    N'## Scale-degree names' + CHAR(10) +
    N'Degree 1 is tonic, 2 supertonic, 3 mediant, 4 subdominant, 5 dominant, 6 submediant, and 7 leading tone when a semitone below the tonic. A lowered 7 a whole tone below the tonic is often called the subtonic.' + CHAR(10) +
    N'Example: In C major, G is degree 5 (dominant); in A natural minor, G is the subtonic.' + CHAR(10) +
    N'Practice: Sing A natural minor, then raise its seventh note and sing A harmonic minor.' + CHAR(10) +
    N'Source: Hutchinson, chapter 3, pp. 11-18.'
WHERE CourseId = @CourseId AND Title = N'The Major Scale';

UPDATE dbo.Lessons
SET Content =
    N'## Learning goals' + CHAR(10) +
    N'- Count interval size from note-letter names.' + CHAR(10) +
    N'- Use semitone distance to identify interval quality.' + CHAR(10) +
    N'- Practice recognizing pitch and interval sounds.' + CHAR(10) +
    N'## Name the interval' + CHAR(10) +
    N'Count both endpoint letter names to find the generic size: C to G spans C-D-E-F-G, so it is some kind of fifth. Then count semitones to determine quality. In a major key, a perfect fifth spans seven semitones; a major third spans four and a minor third spans three.' + CHAR(10) +
    N'## Inversions and altered intervals' + CHAR(10) +
    N'An interval inversion swaps which note is on the bottom. The two interval numbers add to nine for simple intervals: a third inverts to a sixth. Perfect intervals invert to perfect; major and minor qualities trade places. A diminished or augmented interval can be identified by comparing its semitone size with the corresponding perfect, major, or minor form.' + CHAR(10) +
    N'Example: C up to E is a major third (four semitones); E up to C is a minor sixth (eight semitones).' + CHAR(10) +
    N'## Ear-training method' + CHAR(10) +
    N'For two-note drills, listen to the lower pitch, hum it, then compare the second pitch. For single-note drills, replay the tone and match it to a reference note. Use the in-page audio player; the tones are generated locally in your browser.' + CHAR(10) +
    N'Practice: Sing C then G, and describe both its letter span and its sound before selecting an answer.' + CHAR(10) +
    N'Source: Hutchinson, chapter 5, pp. 29-34.'
WHERE CourseId = @CourseId AND Title = N'Intervals and Ear Training';

UPDATE dbo.Lessons
SET Content =
    N'## Learning goals' + CHAR(10) +
    N'- Build triads by stacking alternate scale notes.' + CHAR(10) +
    N'- Distinguish major, minor, diminished, and augmented triads.' + CHAR(10) +
    N'- Read basic lead-sheet chord symbols and inversions.' + CHAR(10) +
    N'## Triad construction' + CHAR(10) +
    N'A triad contains a root, a third, and a fifth. A major triad stacks a major third and a minor third (four then three semitones). A minor triad stacks a minor third and a major third (three then four). Diminished triads contain two minor thirds; augmented triads contain two major thirds.' + CHAR(10) +
    N'## Inversions and symbols' + CHAR(10) +
    N'Root position places the root in the bass. First inversion puts the third in the bass; second inversion puts the fifth in the bass. A lead-sheet symbol names a chord for performance: C means C major, Cm means C minor, and Cdim indicates a diminished triad. Suspended symbols such as Csus4 replace the third with the fourth.' + CHAR(10) +
    N'Example: C-E-G is C major; moving E down one semitone gives C-E-flat-G, C minor.' + CHAR(10) +
    N'## Play and listen' + CHAR(10) +
    N'On guitar, fretboard diagrams show strings and fret positions. Place fingertips close behind the fret, mute strings marked X, and sound strings marked O open. Check each note individually, then strum the chord and listen for a stable blend.' + CHAR(10) +
    N'Practice: Compare C major and C minor by changing only the third; identify the changed note by ear.' + CHAR(10) +
    N'Source: Hutchinson, chapter 6, pp. 35-42.'
WHERE CourseId = @CourseId AND Title = N'Triads and Basic Chords';

MERGE dbo.Lessons AS target
USING (VALUES
 (6, N'Chord Progressions and Harmonic Function',
  N'## Learning goals' + CHAR(10) +
  N'- Read Roman-numeral chord labels in a key.' + CHAR(10) +
  N'- Hear how tonic, predominant, and dominant roles shape a phrase.' + CHAR(10) +
  N'- Recognize common cadence patterns.' + CHAR(10) +
  N'## Chords from a major scale' + CHAR(10) +
  N'Build a triad on each scale degree using only notes in the key. In a major key the resulting pattern is major, minor, minor, major, major, minor, diminished. Roman numerals show scale-degree roots; uppercase commonly marks major quality and lowercase marks minor quality.' + CHAR(10) +
  N'## Function and movement' + CHAR(10) +
  N'Tonic chords provide a point of rest. Predominant chords move away from tonic and prepare the dominant. Dominant harmony creates tension that often resolves to tonic. A cadence is a phrase-ending harmonic motion; V-I is a strong arrival in major, while IV-I can sound more open or gentle.' + CHAR(10) +
  N'Example: In C major, C-F-G-C can be labeled I-IV-V-I and heard as rest, preparation, tension, resolution.' + CHAR(10) +
  N'Practice: Play I-V-vi-IV in C using C-G-Am-F. Sing the bass roots and notice how the loop can support a complete song.' + CHAR(10) +
  N'Source: Hutchinson, chapters 7 and 9, pp. 43-96.'),
 (7, N'Song Form and Accompaniment',
  N'## Learning goals' + CHAR(10) +
  N'- Describe verse-chorus, AABA, and blues forms.' + CHAR(10) +
  N'- Identify arpeggiated, block-chord, and offbeat textures.' + CHAR(10) +
  N'- Arrange contrast between song sections.' + CHAR(10) +
  N'## Form gives music shape' + CHAR(10) +
  N'Song form labels recurring and contrasting sections. Verse-chorus form alternates changing verses with a returning refrain. AABA presents a main idea twice, a contrasting bridge, and a return. A 12-bar blues uses a repeating harmonic frame. Listening for repeated melody, lyrics, harmony, and phrase endings helps you mark section boundaries.' + CHAR(10) +
  N'## Accompaniment choices' + CHAR(10) +
  N'Block chords sound chord tones together. An arpeggio spreads those tones across time. Offbeat accents shift emphasis between the main beats. A bass line can reinforce roots or create its own melodic motion. These textures may use the same harmony while changing energy and feel.' + CHAR(10) +
  N'Example: Keep the chord progression constant, play sustained chords in the verse, then use a stronger strum and higher register in the chorus.' + CHAR(10) +
  N'Practice: Listen to a song and make a section map (A/B/A/B). Note one change in texture or dynamics at each transition.' + CHAR(10) +
  N'Source: Hutchinson, chapters 12 and 14, pp. 139-212.')
) AS source(OrderIndex, Title, Content)
ON target.CourseId = @CourseId AND target.Title = source.Title
WHEN MATCHED THEN UPDATE SET target.Content = source.Content, target.OrderIndex = source.OrderIndex
WHEN NOT MATCHED THEN INSERT (Title, Content, CourseId, OrderIndex, CreatedAt)
    VALUES (source.Title, source.Content, @CourseId, source.OrderIndex, SYSUTCDATETIME());

SELECT @Lesson1 = Id FROM dbo.Lessons WHERE CourseId = @CourseId AND Title = N'The Staff, Clefs, and Note Duration';
SELECT @Lesson2 = Id FROM dbo.Lessons WHERE CourseId = @CourseId AND Title = N'Steps, Accidentals, and Key Signatures';
SELECT @Lesson3 = Id FROM dbo.Lessons WHERE CourseId = @CourseId AND Title = N'The Major Scale';
SELECT @Lesson4 = Id FROM dbo.Lessons WHERE CourseId = @CourseId AND Title = N'Intervals and Ear Training';
SELECT @Lesson5 = Id FROM dbo.Lessons WHERE CourseId = @CourseId AND Title = N'Triads and Basic Chords';

-- Remove only the old external theory-site links from this sample course.
DELETE r
FROM dbo.Resources r
JOIN dbo.Lessons l ON l.Id = r.LessonId
WHERE l.CourseId = @CourseId AND r.Url LIKE N'%musictheory.net%';

-- Ensure self-contained quiz entries exist even if the old seed script
-- previously exited early because its course already existed.
DECLARE @TheoryQuiz INT, @EarQuiz INT;
SELECT @TheoryQuiz = Id FROM dbo.Quizzes WHERE LessonId = @Lesson3 AND Title = N'Major Scale Check';
IF @TheoryQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Major Scale Check', N'Check your understanding of scale construction and degrees.', 0, @Lesson3, SYSUTCDATETIME(), 1);
    SET @TheoryQuiz = SCOPE_IDENTITY();
END
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @TheoryQuiz)
BEGIN
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, Points, OrderIndex)
    VALUES
      (@TheoryQuiz, N'Which pattern builds a major scale?', 0, 0, N'["W-W-H-W-W-W-H","W-H-W-W-W-H-W","H-W-W-H-W-W-W","W-W-W-H-W-W-H"]', 10, 1),
      (@TheoryQuiz, N'How many sharps are in G major?', 0, 1, N'["0","1","2","3"]', 10, 2),
      (@TheoryQuiz, N'What is the 5th scale degree called?', 0, 1, N'["Tonic","Dominant","Mediant","Leading tone"]', 10, 3);
END

SELECT @EarQuiz = Id FROM dbo.Quizzes WHERE LessonId = @Lesson4 AND Title = N'Pitch and Interval Ear Training';
IF @EarQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Pitch and Interval Ear Training', N'Listen to synthesized notes and identify a pitch or interval.', 1, @Lesson4, SYSUTCDATETIME(), 1);
    SET @EarQuiz = SCOPE_IDENTITY();
END
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @EarQuiz AND Type = 3)
BEGIN
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, ToneSequence, CorrectAnswerText, Points, OrderIndex)
    VALUES
      (@EarQuiz, N'Listen to the single note. Which note name do you hear?', 3, N'261.63', N'C', 10, 1),
      (@EarQuiz, N'Listen to the two notes. What interval do you hear?', 3, N'261.63,392.00', N'Perfect 5th', 10, 2),
      (@EarQuiz, N'Listen to the two notes. What interval do you hear?', 3, N'261.63,329.63', N'Major 3rd', 10, 3),
      (@EarQuiz, N'Listen to the single note. Which note name do you hear?', 3, N'293.66', N'D', 10, 4);
END

-- Sight-reading practice inspired by the supplied music-note quiz reference.
-- The note is drawn locally on an SVG staff; answers and attempts remain in Nory.
DECLARE @NoteQuiz INT;
SELECT @NoteQuiz = Id FROM dbo.Quizzes
WHERE LessonId = @Lesson1 AND Title = N'Treble Clef Note Naming';
IF @NoteQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Treble Clef Note Naming', N'Identify natural notes shown on the treble staff. Attempts and scores are tracked in Nory.', 0, @Lesson1, SYSUTCDATETIME(), 1);
    SET @NoteQuiz = SCOPE_IDENTITY();
END
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @NoteQuiz)
BEGIN
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, ToneSequence, Points, OrderIndex)
    VALUES
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 0, N'["C","D","E","F","G","A","B"]', N'staff:treble:C4', 10, 1),
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 1, N'["C","D","E","F","G","A","B"]', N'staff:treble:D4', 10, 2),
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 2, N'["C","D","E","F","G","A","B"]', N'staff:treble:E4', 10, 3),
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 3, N'["C","D","E","F","G","A","B"]', N'staff:treble:F4', 10, 4),
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 4, N'["C","D","E","F","G","A","B"]', N'staff:treble:G4', 10, 5),
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 5, N'["C","D","E","F","G","A","B"]', N'staff:treble:A4', 10, 6),
      (@NoteQuiz, N'Which note is shown on the treble staff?', 0, 6, N'["C","D","E","F","G","A","B"]', N'staff:treble:B4', 10, 7);
END

DECLARE @BassNoteQuiz INT;
SELECT @BassNoteQuiz = Id FROM dbo.Quizzes
WHERE LessonId = @Lesson1 AND Title = N'Bass Clef Note Naming';
IF @BassNoteQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Bass Clef Note Naming', N'Identify natural notes shown on the bass staff. Attempts and scores are tracked in Nory.', 0, @Lesson1, SYSUTCDATETIME(), 1);
    SET @BassNoteQuiz = SCOPE_IDENTITY();
END
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @BassNoteQuiz)
BEGIN
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, ToneSequence, Points, OrderIndex)
    VALUES
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 4, N'["C","D","E","F","G","A","B"]', N'staff:bass:G2', 10, 1),
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 5, N'["C","D","E","F","G","A","B"]', N'staff:bass:A2', 10, 2),
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 6, N'["C","D","E","F","G","A","B"]', N'staff:bass:B2', 10, 3),
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 0, N'["C","D","E","F","G","A","B"]', N'staff:bass:C3', 10, 4),
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 1, N'["C","D","E","F","G","A","B"]', N'staff:bass:D3', 10, 5),
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 2, N'["C","D","E","F","G","A","B"]', N'staff:bass:E3', 10, 6),
      (@BassNoteQuiz, N'Which note is shown on the bass staff?', 0, 3, N'["C","D","E","F","G","A","B"]', N'staff:bass:F3', 10, 7);
END

-- Chord-shape identification is a visual multiple-choice drill like the
-- referenced guitar chord quiz; diagrams are rendered by Nory, not embedded
-- or copied from that service.
DECLARE @ChordShapeQuiz INT;
SELECT @ChordShapeQuiz = Id FROM dbo.Quizzes
WHERE LessonId = @Lesson5 AND Title = N'Guitar Chord Shape Quiz';
IF @ChordShapeQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Guitar Chord Shape Quiz', N'Identify the chord from an original in-app guitar fingering diagram. Results are tracked in Nory.', 0, @Lesson5, SYSUTCDATETIME(), 1);
    SET @ChordShapeQuiz = SCOPE_IDENTITY();
END
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @ChordShapeQuiz)
BEGIN
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, ToneSequence, Points, OrderIndex)
    VALUES
      (@ChordShapeQuiz, N'Which chord does this guitar fingering shape show?', 0, 0, N'["E major","A major","D major","E minor"]', N'diagram:022100', 10, 1),
      (@ChordShapeQuiz, N'Which chord does this guitar fingering shape show?', 0, 1, N'["E major","A major","D major","E minor"]', N'diagram:x02220', 10, 2),
      (@ChordShapeQuiz, N'Which chord does this guitar fingering shape show?', 0, 2, N'["E major","A major","D major","E minor"]', N'diagram:xx0232', 10, 3),
      (@ChordShapeQuiz, N'Which chord does this guitar fingering shape show?', 0, 3, N'["E major","A major","D major","E minor"]', N'diagram:022000', 10, 4);
END

-- Chord recognition practice: synthesized chord tones are played together
-- in the browser; no external quiz or audio service is required.
DECLARE @ChordQuiz INT;
SELECT @ChordQuiz = Id FROM dbo.Quizzes
WHERE LessonId = @Lesson5 AND Title = N'Guitar Chord Ear Training';
IF @ChordQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Guitar Chord Ear Training', N'Listen to a synthesized guitar chord and identify its name. Your score is tracked in Nory.', 1, @Lesson5, SYSUTCDATETIME(), 1);
    SET @ChordQuiz = SCOPE_IDENTITY();
END
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @ChordQuiz)
BEGIN
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, ToneSequence, Points, OrderIndex)
    VALUES
      (@ChordQuiz, N'Listen to the chord, then choose its name.', 0, 0, N'["E major","A major","D major","E minor"]', N'chord:82.41,123.47,164.81,207.65,246.94,329.63', 10, 1),
      (@ChordQuiz, N'Listen to the chord, then choose its name.', 0, 1, N'["E major","A major","D major","E minor"]', N'chord:110.00,164.81,220.00,277.18,329.63', 10, 2),
      (@ChordQuiz, N'Listen to the chord, then choose its name.', 0, 2, N'["E major","A major","D major","E minor"]', N'chord:146.83,220.00,293.66,369.99', 10, 3),
      (@ChordQuiz, N'Listen to the chord, then choose its name.', 0, 3, N'["E major","A major","D major","E minor"]', N'chord:82.41,123.47,164.81,196.00,246.94,329.63', 10, 4);
END

-- Chord-only entry based on the cited public chord reference. No lyrics or
-- tablature are copied. Tempo is a learner-adjustable starting point, not
-- asserted as the recording's measured tempo.
IF NOT EXISTS (SELECT 1 FROM dbo.ChordSongs WHERE Title = N'Najeek' AND Artist = N'Bartika Eam Rai' AND Instrument = N'Guitar')
BEGIN
    INSERT dbo.ChordSongs
        (Title, Artist, Instrument, [Key], CapoFret, TempoBpm, TempoNote, ChordMap, StrummingPattern, AttributionUrl, AddedAt)
    VALUES
        (N'Najeek', N'Bartika Eam Rai', N'Guitar', N'G', 3, 100,
         N'Starter metronome setting only; adjust by listening to the recording.',
         N'Intro: G – Cadd9 (repeat)' + CHAR(13) + CHAR(10) +
         N'Verse: G – Cadd9 (repeat)' + CHAR(13) + CHAR(10) +
         N'Refrain: C – Em – D – Em' + CHAR(13) + CHAR(10) +
         N'Chord palette: G, Cadd9, C, Em, D',
         NULL, N'https://tabs.ultimate-guitar.com/tab/bartika-eam-rai/najeek-chords-1897678', SYSUTCDATETIME());
END
ELSE
    UPDATE dbo.ChordSongs
    SET AttributionUrl = N'https://tabs.ultimate-guitar.com/tab/bartika-eam-rai/najeek-chords-1897678'
    WHERE Title = N'Najeek' AND Artist = N'Bartika Eam Rai' AND Instrument = N'Guitar';
GO

PRINT 'Local lesson content, theory and ear-training quizzes, and Najeek guitar chord guide are ready.';
GO
