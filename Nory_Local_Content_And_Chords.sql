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
    (SELECT TOP (1) Id FROM dbo.Courses WHERE Title IN (N'Music Theory Fundamentals', N'Music Theory Foundations') ORDER BY CASE WHEN Title = N'Music Theory Foundations' THEN 0 ELSE 1 END);
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

-- Expand the starter into a sequenced curriculum. The original course keeps
-- its primary key and existing lesson/completion history; new courses are
-- inserted by title and can be seeded repeatedly without duplicate records.
UPDATE dbo.Courses
SET Title = N'Music Theory Foundations',
    Description = N'Course 1 of the Nory theory path: notation, rhythm, key signatures, scales, intervals, and introductory harmony. Original lesson summaries cite Robert Hutchinson''s openly licensed textbook; read the full source at /references/MusicTheory.pdf.'
WHERE Id = @CourseId;

DECLARE @CurriculumCourses TABLE (Title NVARCHAR(100) PRIMARY KEY, Description NVARCHAR(500));
INSERT @CurriculumCourses (Title, Description) VALUES
(N'Harmony and Chord Function', N'Course 2: build triads and seventh chords, read Roman numerals, and hear harmonic function and cadences. Lessons are original summaries based on Hutchinson''s textbook; full source: /references/MusicTheory.pdf.'),
(N'Melody, Form, and Accompaniment', N'Course 3: shape melodies, recognize phrases and musical forms, and choose accompaniment textures. Lessons are original summaries based on Hutchinson''s textbook; full source: /references/MusicTheory.pdf.'),
(N'Chromatic Harmony and Voice Leading', N'Course 4: explore chromatic color, modulation, smooth voice leading, and counterpoint. Lessons are original summaries based on Hutchinson''s textbook; full source: /references/MusicTheory.pdf.'),
(N'Jazz and Modern Music Theory', N'Course 5: study jazz harmony and modern compositional approaches, including twelve-tone and minimalist ideas. Lessons are original summaries based on Hutchinson''s textbook; full source: /references/MusicTheory.pdf.'),
(N'Beginner Guitar Chords', N'Learn to read guitar chord diagrams, form clear open chords, move between shapes, and practise progressions. Original guitar instruction complements the Nory chord-song library.');

MERGE dbo.Courses AS target
USING @CurriculumCourses AS source ON target.Title = source.Title
WHEN MATCHED THEN UPDATE SET target.Description = source.Description, target.InstructorId = @InstructorId
WHEN NOT MATCHED THEN INSERT (Title, Description, InstructorId, StartDate, EndDate, IsActive, ImageUrl, CreatedAt)
VALUES (source.Title, source.Description, @InstructorId, CONVERT(date, SYSUTCDATETIME()), DATEADD(month, 6, SYSUTCDATETIME()), 1, NULL, SYSUTCDATETIME());

DECLARE @CurriculumLessons TABLE (CourseTitle NVARCHAR(100), OrderIndex INT, Title NVARCHAR(100), Content NVARCHAR(2000));
INSERT @CurriculumLessons (CourseTitle, OrderIndex, Title, Content) VALUES
(N'Harmony and Chord Function',1,N'Triads and Inversions',N'## Learning goals' + CHAR(10) + N'- Build major, minor, diminished, and augmented triads.' + CHAR(10) + N'- Identify root position and inversions.' + CHAR(10) + N'## Build the triad' + CHAR(10) + N'A triad combines a root, third, and fifth. Major quality stacks four then three semitones; minor stacks three then four. Diminished uses two minor thirds; augmented uses two major thirds. Spell chord tones with distinct letter names, then check the semitone pattern.' + CHAR(10) + N'## Inversion' + CHAR(10) + N'Root position places the root in the bass. First inversion places the third in the bass; second inversion places the fifth in the bass. The chord identity stays the same even as the bass note changes.' + CHAR(10) + N'Example: C-E-G is C major; E-G-C is C major in first inversion.' + CHAR(10) + N'Practice: Build a D-minor triad, then rearrange its notes so the third is lowest.' + CHAR(10) + N'Source: Hutchinson, chapters 6-7.'),
(N'Harmony and Chord Function',2,N'Seventh Chords and Roman Numerals',N'## Learning goals' + CHAR(10) + N'- Extend triads with a seventh.' + CHAR(10) + N'- Read scale-degree chord labels.' + CHAR(10) + N'## Seventh chords' + CHAR(10) + N'A seventh chord adds a chordal seventh above a triad. Common qualities include major seventh, dominant seventh, minor seventh, half-diminished, and fully diminished. A dominant seventh combines a major triad with a minor seventh and commonly resolves toward tonic.' + CHAR(10) + N'## Roman-numeral analysis' + CHAR(10) + N'Roman numerals show the chord root as a scale degree. Uppercase commonly indicates major quality and lowercase indicates minor; diminished chords use a degree sign. In a major key the diatonic triad pattern is I, ii, iii, IV, V, vi, vii°.' + CHAR(10) + N'Example: In C major, D-F-A is ii and G-B-D-F is V7.' + CHAR(10) + N'Practice: Write the diatonic triads in G major and label each with a Roman numeral.' + CHAR(10) + N'Source: Hutchinson, chapters 7-9.'),
(N'Harmony and Chord Function',3,N'Cadences and Harmonic Function',N'## Learning goals' + CHAR(10) + N'- Distinguish tonic, predominant, and dominant roles.' + CHAR(10) + N'- Recognize common cadence patterns.' + CHAR(10) + N'## Functional motion' + CHAR(10) + N'Tonic harmony sounds comparatively settled. Predominant harmony moves away from tonic and prepares dominant. Dominant harmony creates directed tension, often resolving to tonic. Function depends on musical context, not only a chord name.' + CHAR(10) + N'## Cadences' + CHAR(10) + N'A cadence closes or pauses a phrase. V-I is an authentic cadence; V moving to vi creates a deceptive cadence; IV-I is a plagal cadence; ending on V produces a half cadence. Phrase melody and bass motion help clarify the arrival.' + CHAR(10) + N'Example: In C major, F-G-C moves predominant to dominant to tonic.' + CHAR(10) + N'Practice: Play I-IV-V-I in C and sing the bass roots.' + CHAR(10) + N'Source: Hutchinson, chapters 8-10.'),
(N'Melody, Form, and Accompaniment',1,N'Melody, Motive, and Phrase',N'## Learning goals' + CHAR(10) + N'- Describe melodic contour and range.' + CHAR(10) + N'- Recognize motives and phrase endings.' + CHAR(10) + N'## Melodic shape' + CHAR(10) + N'Melody is a sequence of pitches perceived as a musical line. Contour describes whether it rises, falls, or repeats; range is the distance between its lowest and highest notes. Repeated rhythmic or pitch ideas create motives that listeners can recognize.' + CHAR(10) + N'## Phrases' + CHAR(10) + N'A phrase is a musical thought, often ending with a breath, pause, cadence, or longer note. Antecedent-consequent phrases commonly pair an open-sounding question with a more conclusive answer.' + CHAR(10) + N'Example: Repeat a four-note motive, then change its final note to create a response.' + CHAR(10) + N'Practice: Sketch the contour of a melody using up, down, and same.' + CHAR(10) + N'Source: Hutchinson, chapters 11-12.'),
(N'Melody, Form, and Accompaniment',2,N'Song Forms and Section Maps',N'## Learning goals' + CHAR(10) + N'- Identify repeated and contrasting sections.' + CHAR(10) + N'- Map a song with letter labels.' + CHAR(10) + N'## Hearing form' + CHAR(10) + N'Form organizes repetition and contrast over time. Strophic songs reuse music for successive verses. Verse-chorus form alternates changing verses with a recurring refrain. AABA presents a theme twice, a contrasting bridge, and a return. Twelve-bar blues follows a recurring harmonic frame.' + CHAR(10) + N'Listen for melody, harmony, rhythm, instrumentation, and lyrics to locate section boundaries. A letter map labels similar sections with the same letter; a changed section gets a new one.' + CHAR(10) + N'Example: A-B-A-B can describe a verse and chorus that alternate.' + CHAR(10) + N'Practice: Make a section map for a song and note one audible change at each transition.' + CHAR(10) + N'Source: Hutchinson, chapters 12-14.'),
(N'Melody, Form, and Accompaniment',3,N'Accompaniment and Texture',N'## Learning goals' + CHAR(10) + N'- Compare block chords, arpeggios, and bass lines.' + CHAR(10) + N'- Use texture to create section contrast.' + CHAR(10) + N'## Accompaniment choices' + CHAR(10) + N'Block chords sound chord tones together. An arpeggio spreads chord tones through time. A walking or patterned bass line can reinforce roots or create independent motion. Texture describes how musical parts combine, including their density and interaction.' + CHAR(10) + N'Keep the harmony stable while changing rhythm, register, articulation, or dynamics to support a new section. A quieter verse and fuller chorus can create contrast without changing the chord progression.' + CHAR(10) + N'Example: Play the verse as a light arpeggio, then use a steady strum in the chorus.' + CHAR(10) + N'Practice: Accompany one progression in two contrasting textures.' + CHAR(10) + N'Source: Hutchinson, chapters 13-14.'),
(N'Chromatic Harmony and Voice Leading',1,N'Chromatic Notes and Applied Harmony',N'## Learning goals' + CHAR(10) + N'- Recognize notes outside the home key.' + CHAR(10) + N'- Hear how chromatic notes intensify motion.' + CHAR(10) + N'## Chromatic color' + CHAR(10) + N'A chromatic note lies outside the prevailing diatonic collection. It may pass between chord tones, decorate a note, or signal a temporary tonic. Applied dominants briefly direct attention toward a chord other than the home tonic.' + CHAR(10) + N'Use spelling and context to distinguish a chromatic alteration from a modulation. Follow the altered note into its resolution and listen for the new tension it creates.' + CHAR(10) + N'Example: In C major, D7 can point toward G by raising F to F-sharp.' + CHAR(10) + N'Practice: Find the altered tone in D7 and resolve it by step.' + CHAR(10) + N'Source: Hutchinson, chapters 16-20.'),
(N'Chromatic Harmony and Voice Leading',2,N'Modulation and Key Relationships',N'## Learning goals' + CHAR(10) + N'- Explain the difference between tonicization and modulation.' + CHAR(10) + N'- Find clues that establish a new key.' + CHAR(10) + N'## A change of tonal center' + CHAR(10) + N'Tonicization emphasizes a chord briefly, often with its applied dominant. Modulation establishes a new tonic for a longer span. A pivot chord can belong to both keys and help connect them smoothly.' + CHAR(10) + N'Listen for repeated accidentals, cadences in the new key, and sustained emphasis on a new tonic. A single chromatic note alone does not prove that a modulation occurred.' + CHAR(10) + N'Example: A cadence that repeatedly resolves to G can establish G major after C major.' + CHAR(10) + N'Practice: Mark the first cadence that makes the new tonic sound convincing.' + CHAR(10) + N'Source: Hutchinson, chapters 20-22.'),
(N'Chromatic Harmony and Voice Leading',3,N'Voice Leading and Counterpoint',N'## Learning goals' + CHAR(10) + N'- Move voices smoothly between chords.' + CHAR(10) + N'- Describe independent melodic lines.' + CHAR(10) + N'## Connected parts' + CHAR(10) + N'Voice leading connects notes in one part to notes in the next. Common tones can remain in place; other voices often move by step. Avoiding awkward leaps and parallel perfect fifths or octaves can help preserve independent lines in common-practice counterpoint.' + CHAR(10) + N'Counterpoint combines melodies that are individually coherent and work together. The rules depend on style: later music may intentionally use effects that earlier exercises avoid.' + CHAR(10) + N'Example: From C major to A minor, keep C and E while moving G to A.' + CHAR(10) + N'Practice: Connect two triads with the smallest motion possible in each voice.' + CHAR(10) + N'Source: Hutchinson, chapters 22-26.'),
(N'Jazz and Modern Music Theory',1,N'Jazz Chords and Lead-Sheet Symbols',N'## Learning goals' + CHAR(10) + N'- Read common extended-chord symbols.' + CHAR(10) + N'- Hear seventh chords as color and function.' + CHAR(10) + N'## Chord extensions' + CHAR(10) + N'Jazz harmony commonly uses seventh chords and extensions such as ninths, elevenths, and thirteenths. Lead-sheet symbols give performers a chord root and quality, with alterations or omissions indicated by suffixes. A symbol is a compact instruction, not a single mandatory voicing.' + CHAR(10) + N'Dominant sevenths can resolve by fifth to tonic or move through a cycle. Players choose voicings that fit their instrument, register, and ensemble.' + CHAR(10) + N'Example: G7 names G-B-D-F; Cmaj7 names C-E-G-B.' + CHAR(10) + N'Practice: Spell Dm7 and identify its root, third, fifth, and seventh.' + CHAR(10) + N'Source: Hutchinson, chapter 31.'),
(N'Jazz and Modern Music Theory',2,N'Jazz Harmony and Improvisation',N'## Learning goals' + CHAR(10) + N'- Trace ii-V-I motion.' + CHAR(10) + N'- Relate improvisation to harmony and rhythm.' + CHAR(10) + N'## Harmonic direction' + CHAR(10) + N'The ii-V-I progression links predominant and dominant function to tonic and is central to many jazz standards. Chord tones outline harmony; approach tones and passing notes connect them. Rhythm, articulation, and rests shape a solo as much as pitch choice.' + CHAR(10) + N'Listen to the bass and guide tones while a progression plays. Chord symbols describe harmony, while improvisers create a melody that responds to it.' + CHAR(10) + N'Example: In C major, Dm7-G7-Cmaj7 is ii7-V7-Imaj7.' + CHAR(10) + N'Practice: Play the third and seventh of each chord through a ii-V-I.' + CHAR(10) + N'Source: Hutchinson, chapter 31.'),
(N'Jazz and Modern Music Theory',3,N'Twelve-Tone and Minimalist Ideas',N'## Learning goals' + CHAR(10) + N'- Describe a tone row as an ordered pitch collection.' + CHAR(10) + N'- Hear repetition and gradual change in minimalism.' + CHAR(10) + N'## Organizing pitch' + CHAR(10) + N'Twelve-tone technique organizes all twelve pitch classes in a chosen row before repeating them, often using prime, inversion, retrograde, and retrograde inversion forms. It is one way composers organize pitch, not a replacement for listening to rhythm, register, and timbre.' + CHAR(10) + N'Minimalist music often uses repeated patterns and gradual processes. Small changes in phase, harmony, or orchestration can transform a texture over time.' + CHAR(10) + N'Example: A short repeating figure can shift its accents while keeping the same notes.' + CHAR(10) + N'Practice: Listen for one element that changes and one that remains constant.' + CHAR(10) + N'Source: Hutchinson, later chapters on twentieth-century techniques.'),
(N'Beginner Guitar Chords',1,N'Reading Guitar Chord Diagrams',N'## Learning goals' + CHAR(10) + N'- Match diagram strings to guitar strings.' + CHAR(10) + N'- Read open, muted, and fretted strings.' + CHAR(10) + N'## Read the shape' + CHAR(10) + N'A chord diagram shows the guitar neck from the player-facing view: vertical lines are strings and horizontal lines are frets. The thick nut is above the first fret. In Nory diagrams the six-string sequence runs from low E to high E. An O means play an open string; X means mute it; a dot marks a fretted note.' + CHAR(10) + N'Press close behind each fret, use fingertips, and sound strings one at a time to check clarity.' + CHAR(10) + N'Example: 022100 means low E open, A fret 2, D fret 2, G fret 1, B open, high E open.' + CHAR(10) + N'Practice: Read the E-major shape and name its open strings.'),
(N'Beginner Guitar Chords',2,N'Open Major and Minor Chords',N'## Learning goals' + CHAR(10) + N'- Form common open-position chord shapes.' + CHAR(10) + N'- Compare major and minor chord color.' + CHAR(10) + N'## First chord family' + CHAR(10) + N'Open chords combine fretted notes with one or more unfretted strings. Start with E, Em, A, Am, D, and C shapes. Keep the thumb relaxed behind the neck, curve the fingers, and avoid touching neighboring strings.' + CHAR(10) + N'Major and minor are different triad qualities. In a minor shape the third is lowered relative to its major counterpart, changing the chord color.' + CHAR(10) + N'Example: E major 022100 and E minor 022000 differ at the G string.' + CHAR(10) + N'Practice: Form E and Em in turn; pluck each string and correct any buzz.'),
(N'Beginner Guitar Chords',3,N'Chord Changes and Steady Tempo',N'## Learning goals' + CHAR(10) + N'- Move between chord shapes efficiently.' + CHAR(10) + N'- Practise with an adjustable metronome.' + CHAR(10) + N'## Make the transition' + CHAR(10) + N'Choose a shared anchor finger where possible, lift only as far as needed, and prepare the next shape before the beat. Start with one slow strum per bar, then add a regular down-strum on each beat.' + CHAR(10) + N'A metronome provides evenly spaced beats. Start at a comfortable BPM, make a clean change for several bars, then increase speed gradually without losing timing or tone.' + CHAR(10) + N'Example: Change between G and C at 60 BPM, one chord per four beats.' + CHAR(10) + N'Practice: Raise the tempo by 4 BPM only after four clean changes.'),
(N'Beginner Guitar Chords',4,N'Progressions, Strumming, and Song Practice',N'## Learning goals' + CHAR(10) + N'- Play a repeating chord progression in time.' + CHAR(10) + N'- Apply a strumming pattern to a song map.' + CHAR(10) + N'## Put chords together' + CHAR(10) + N'A progression is a planned sequence of chords. Use a relaxed wrist and keep the beat steady; strum only the strings that belong in the chord when practical. Chord-only song maps show section progressions without reproducing lyrics.' + CHAR(10) + N'Practise a progression slowly with a metronome before playing along with a recording. Increase speed only when changes are clear and even.' + CHAR(10) + N'Example: G-Cadd9 can support a two-chord practice loop.' + CHAR(10) + N'Practice: Play a verse progression at a slow tempo, then use the chord-song library for a section map.');

MERGE dbo.Lessons AS target
USING (
    SELECT c.Id AS CourseId, l.OrderIndex, l.Title, l.Content
    FROM @CurriculumLessons l
    JOIN dbo.Courses c ON c.Title = l.CourseTitle
) AS source ON target.CourseId = source.CourseId AND target.Title = source.Title
WHEN MATCHED THEN UPDATE SET target.Content = source.Content, target.OrderIndex = source.OrderIndex
WHEN NOT MATCHED THEN INSERT (Title, Content, CourseId, OrderIndex, CreatedAt)
VALUES (source.Title, source.Content, source.CourseId, source.OrderIndex, SYSUTCDATETIME());

-- Expand the existing starter checks when present. These insert guards make
-- the script safe to rerun without replacing completed attempts or answers.
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @TheoryQuiz AND OrderIndex = 4)
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, Points, OrderIndex)
    VALUES (@TheoryQuiz, N'Which two scale degrees form half steps in a major scale?', 0, 0, N'["3-4 and 7-1","2-3 and 5-6","1-2 and 4-5","4-5 and 6-7"]', 10, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @TheoryQuiz AND OrderIndex = 5)
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, Points, OrderIndex)
    VALUES (@TheoryQuiz, N'In C major, which chord is the dominant triad?', 0, 2, N'["C-E-G","F-A-C","G-B-D","A-C-E"]', 10, 5);
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @EarQuiz AND OrderIndex = 5)
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, ToneSequence, CorrectAnswerText, Points, OrderIndex)
    VALUES (@EarQuiz, N'Listen to the two notes. What interval do you hear?', 3, N'261.63,349.23', N'Perfect 4th', 10, 5);

DECLARE @NewQuizzes TABLE (CourseTitle NVARCHAR(100), LessonTitle NVARCHAR(100), QuizTitle NVARCHAR(100), QuizType INT);
INSERT @NewQuizzes VALUES
(N'Harmony and Chord Function',N'Cadences and Harmonic Function',N'Harmony and Cadence Review',0),
(N'Melody, Form, and Accompaniment',N'Accompaniment and Texture',N'Melody and Form Review',0),
(N'Chromatic Harmony and Voice Leading',N'Voice Leading and Counterpoint',N'Chromatic Harmony Review',0),
(N'Jazz and Modern Music Theory',N'Twelve-Tone and Minimalist Ideas',N'Jazz and Modern Theory Review',0),
(N'Beginner Guitar Chords',N'Progressions, Strumming, and Song Practice',N'Guitar Chord Skills Check',0);

MERGE dbo.Quizzes AS target
USING (
    SELECT l.Id AS LessonId, q.QuizTitle, q.CourseTitle, q.QuizType
    FROM @NewQuizzes q
    JOIN dbo.Courses c ON c.Title = q.CourseTitle
    JOIN dbo.Lessons l ON l.CourseId = c.Id AND l.Title = q.LessonTitle
) AS source ON target.LessonId = source.LessonId AND target.Title = source.QuizTitle
WHEN MATCHED THEN UPDATE SET target.Description = N'Check the lesson objectives with applied questions and feedback.', target.Type = source.QuizType, target.IsActive = 1
WHEN NOT MATCHED THEN INSERT (Title, Description, Type, LessonId, CreatedAt, IsActive)
VALUES (source.QuizTitle, N'Check the lesson objectives with applied questions and feedback.', source.QuizType, source.LessonId, SYSUTCDATETIME(), 1);

DECLARE @QuestionBank TABLE (QuizTitle NVARCHAR(100), OrderIndex INT, QuestionText NVARCHAR(500), Type INT, CorrectAnswerIndex INT, OptionsJson NVARCHAR(1000), ToneSequence NVARCHAR(200), Points INT);
INSERT @QuestionBank VALUES
(N'Harmony and Cadence Review',1,N'Which semitone pattern forms a major triad?',0,0,N'["4 then 3","3 then 4","3 then 3","4 then 4"]',NULL,10),
(N'Harmony and Cadence Review',2,N'Which Roman numeral is diminished in a major key?',0,3,N'["I","IV","V","vii°"]',NULL,10),
(N'Harmony and Cadence Review',3,N'In C major, which progression is a V-I cadence?',0,1,N'["F-C","G-C","C-G","A-F"]',NULL,10),
(N'Harmony and Cadence Review',4,N'Which chord tone is in the bass in first inversion?',0,1,N'["Root","Third","Fifth","Seventh"]',NULL,10),
(N'Harmony and Cadence Review',5,N'Which tones make a G7 chord?',0,2,N'["G-B-D","G-B-D-F","G-C-D-F","G-B-E-F"]',NULL,10),
(N'Melody and Form Review',1,N'What does melodic contour describe?',0,0,N'["The direction of a melody","Its loudness only","The chord root","The instrument family"]',NULL,10),
(N'Melody and Form Review',2,N'Which form returns to its opening section after a contrasting bridge?',0,2,N'["Strophic","Verse-chorus","AABA","Through-composed"]',NULL,10),
(N'Melody and Form Review',3,N'What is an arpeggio?',0,1,N'["Chord tones played together","Chord tones spread through time","A repeated bass note","A type of cadence"]',NULL,10),
(N'Melody and Form Review',4,N'In an A-B-A-B map, which sections return?',0,3,N'["Only A","Only B","Neither","Both A and B"]',NULL,10),
(N'Melody and Form Review',5,N'Which change can create contrast without changing harmony?',0,1,N'["Change texture or dynamics","Remove the beat","Rename the key","Change the tonic by definition"]',NULL,10),
(N'Chromatic Harmony Review',1,N'What makes a note chromatic in a key?',0,1,N'["It is the tonic","It falls outside the diatonic collection","It is always a rest","It is always the leading tone"]',NULL,10),
(N'Chromatic Harmony Review',2,N'In C major, what can D7 commonly tonicize?',0,2,N'["C","F","G","A-flat"]',NULL,10),
(N'Chromatic Harmony Review',3,N'Which statement best distinguishes modulation?',0,0,N'["A new tonic is established for a span","Any single accidental changes the key","A chord is inverted","The tempo changes"]',NULL,10),
(N'Chromatic Harmony Review',4,N'What is a useful voice-leading goal in common-practice exercises?',0,3,N'["Maximize every leap","Keep every note the same","Double all leading tones","Move voices smoothly and avoid forbidden parallels"]',NULL,10),
(N'Chromatic Harmony Review',5,N'What does counterpoint combine?',0,1,N'["Only percussion patterns","Independent melodic lines","One melody and silence","A scale and a tempo"]',NULL,10),
(N'Jazz and Modern Theory Review',1,N'Which progression is a ii-V-I in C major?',0,0,N'["Dm7-G7-Cmaj7","C-F-G","Am-Dm-G","F-G-Am"]',NULL,10),
(N'Jazz and Modern Theory Review',2,N'Which pitches spell G7?',0,2,N'["G-B-D","G-C-D-F","G-B-D-F","G-A-D-F"]',NULL,10),
(N'Jazz and Modern Theory Review',3,N'What is a tone row?',0,1,N'["A chord inversion","An ordered series of pitch classes","A rhythmic meter","A guitar tuning"]',NULL,10),
(N'Jazz and Modern Theory Review',4,N'What is often characteristic of minimalist music?',0,3,N'["Frequent key changes only","No repetition","A fixed tempo with no variation","Repeating patterns and gradual change"]',NULL,10),
(N'Jazz and Modern Theory Review',5,N'Which can be a form of a twelve-tone row?',0,0,N'["Retrograde","Cadence","Arpeggio only","Chorus"]',NULL,10),
(N'Guitar Chord Skills Check',1,N'In a chord diagram, what does X above a string mean?',0,2,N'["Play it open","Fret it at the nut","Mute or do not play it","Bend it"]',N'diagram:x02220',10),
(N'Guitar Chord Skills Check',2,N'Which shape is E minor in low-E to high-E order?',0,1,N'["022100","022000","x02220","xx0232"]',N'diagram:022000',10),
(N'Guitar Chord Skills Check',3,N'Which two open-position shapes differ by the G-string fret?',0,0,N'["E major and E minor","A major and D major","C and G","D and A"]',N'diagram:022100',10),
(N'Guitar Chord Skills Check',4,N'Which practice change is best when chord changes are unclear?',0,3,N'["Increase BPM quickly","Strum harder","Ignore the beat","Lower tempo and make clean changes"]',N'chord:82.41,123.47,164.81,207.65,246.94,329.63',10),
(N'Guitar Chord Skills Check',5,N'What should a player do before raising the metronome speed?',0,2,N'["Skip beats","Change chords only once","Play several clean changes steadily","Mute every string"]',NULL,10);

INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, ToneSequence, Points, OrderIndex)
SELECT q.Id, b.QuestionText, b.Type, b.CorrectAnswerIndex, b.OptionsJson, b.ToneSequence, b.Points, b.OrderIndex
FROM @QuestionBank b
JOIN dbo.Quizzes q ON q.Title = b.QuizTitle
WHERE NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions existing WHERE existing.QuizId = q.Id AND existing.OrderIndex = b.OrderIndex);

-- Ear identification can be answered from choices while replaying synthesized
-- tones; staff questions are visually notated by the existing SVG renderer.
DECLARE @FoundationsId INT = (SELECT Id FROM dbo.Courses WHERE Id = @CourseId);
DECLARE @EarCourseQuiz INT;
SELECT @EarCourseQuiz = Id FROM dbo.Quizzes WHERE LessonId = @Lesson4 AND Title = N'Natural Note Ear Training';
IF @EarCourseQuiz IS NULL
BEGIN
    INSERT dbo.Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
    VALUES (N'Natural Note Ear Training', N'Listen to a synthesized pitch, replay it as needed, and identify the note.', 1, @Lesson4, SYSUTCDATETIME(), 1);
    SET @EarCourseQuiz = SCOPE_IDENTITY();
END;
IF NOT EXISTS (SELECT 1 FROM dbo.QuizQuestions WHERE QuizId = @EarCourseQuiz)
    INSERT dbo.QuizQuestions (QuizId, QuestionText, Type, CorrectAnswerIndex, OptionsJson, ToneSequence, Points, OrderIndex)
    VALUES
    (@EarCourseQuiz,N'Listen and choose the note.',0,0,N'["C","D","E","F"]',N'261.63',10,1),
    (@EarCourseQuiz,N'Listen and choose the note.',0,1,N'["C","D","E","G"]',N'293.66',10,2),
    (@EarCourseQuiz,N'Listen and choose the note.',0,2,N'["A","B","E","D"]',N'329.63',10,3),
    (@EarCourseQuiz,N'Listen and choose the note.',0,3,N'["C","D","E","F"]',N'349.23',10,4),
    (@EarCourseQuiz,N'Listen and choose the note.',0,3,N'["C","D","E","G"]',N'392.00',10,5);

PRINT 'Multi-course Music Theory curriculum, guitar learning path, quizzes, and chord song guide are ready.';
GO
