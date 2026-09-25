-- =====================================================================
-- Nory - Sample Content Seed Script
-- Creates one sample course ("Music Theory Fundamentals") with lessons
-- that follow musictheory.net's own lesson sequence, and attaches each
-- lesson to the matching musictheory.net page as an external Resource
-- (Type = 4 / ExternalLink) so students have somewhere real to practice
-- ear training and theory exercises.
--
-- Run this in SSMS AFTER Nory_Database_Script.sql, against the Nory
-- database. Requires that admin@norymusic.com already exists (it's
-- created automatically the first time you run the app).
-- =====================================================================

USE [Nory]
GO

DECLARE @InstructorId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE Email = 'admin@norymusic.com');

IF @InstructorId IS NULL
BEGIN
    PRINT 'admin@norymusic.com not found - run the app at least once first so the seeded admin account exists, then re-run this script.';
    RETURN;
END

IF EXISTS (SELECT 1 FROM Courses WHERE Title = 'Music Theory Fundamentals')
BEGIN
    PRINT 'Sample course already exists - skipping.';
    RETURN;
END

DECLARE @CourseId INT;

INSERT INTO Courses (Title, Description, InstructorId, StartDate, EndDate, IsActive, ImageUrl, CreatedAt)
VALUES (
    N'Music Theory Fundamentals',
    N'A practical introduction to reading music, scales, intervals, and chords - paired with musictheory.net for ear training and interactive exercises.',
    @InstructorId,
    GETUTCDATE(),
    DATEADD(MONTH, 3, GETUTCDATE()),
    1,
    NULL,
    GETUTCDATE()
);
SET @CourseId = SCOPE_IDENTITY();

-- ---------------------------------------------------------------------
-- Lesson 1: The Staff, Clefs, and Note Duration
-- ---------------------------------------------------------------------
DECLARE @Lesson1 INT;
INSERT INTO Lessons (Title, Content, CourseId, OrderIndex, VideoUrl, AudioUrl, NotationUrl, CreatedAt)
VALUES (
    N'The Staff, Clefs, and Note Duration',
    N'Learn how pitches are notated on the staff using treble and bass clefs, and how note shapes indicate duration.',
    @CourseId, 1, NULL, NULL, NULL, GETUTCDATE()
);
SET @Lesson1 = SCOPE_IDENTITY();

INSERT INTO Resources (Title, Description, Type, Url, LessonId, UploadedAt) VALUES
(N'The Staff, Clefs, and Ledger Lines', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/10', @Lesson1, GETUTCDATE()),
(N'Note Duration', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/11', @Lesson1, GETUTCDATE()),
(N'Note Reading Trainer', N'musictheory.net exercise', 4, N'https://www.musictheory.net/exercises', @Lesson1, GETUTCDATE());

-- ---------------------------------------------------------------------
-- Lesson 2: Steps, Accidentals, and Key Signatures
-- ---------------------------------------------------------------------
DECLARE @Lesson2 INT;
INSERT INTO Lessons (Title, Content, CourseId, OrderIndex, VideoUrl, AudioUrl, NotationUrl, CreatedAt)
VALUES (
    N'Steps, Accidentals, and Key Signatures',
    N'Understand half steps, whole steps, sharps and flats, and how key signatures are built and ordered.',
    @CourseId, 2, NULL, NULL, NULL, GETUTCDATE()
);
SET @Lesson2 = SCOPE_IDENTITY();

INSERT INTO Resources (Title, Description, Type, Url, LessonId, UploadedAt) VALUES
(N'Steps and Accidentals', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/20', @Lesson2, GETUTCDATE()),
(N'Key Signatures', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/24', @Lesson2, GETUTCDATE()),
(N'Key Signature Identification Trainer', N'musictheory.net exercise', 4, N'https://www.musictheory.net/exercises', @Lesson2, GETUTCDATE());

-- ---------------------------------------------------------------------
-- Lesson 3: The Major Scale
-- ---------------------------------------------------------------------
DECLARE @Lesson3 INT;
INSERT INTO Lessons (Title, Content, CourseId, OrderIndex, VideoUrl, AudioUrl, NotationUrl, CreatedAt)
VALUES (
    N'The Major Scale',
    N'Learn the whole-step/half-step formula that builds a major scale from any starting note.',
    @CourseId, 3, NULL, NULL, NULL, GETUTCDATE()
);
SET @Lesson3 = SCOPE_IDENTITY();

INSERT INTO Resources (Title, Description, Type, Url, LessonId, UploadedAt) VALUES
(N'The Major Scale', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/21', @Lesson3, GETUTCDATE()),
(N'Scale Degrees', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/23', @Lesson3, GETUTCDATE());

-- ---------------------------------------------------------------------
-- Lesson 4: Intervals
-- ---------------------------------------------------------------------
DECLARE @Lesson4 INT;
INSERT INTO Lessons (Title, Content, CourseId, OrderIndex, VideoUrl, AudioUrl, NotationUrl, CreatedAt)
VALUES (
    N'Intervals',
    N'Measure the distance between two notes, both generically (by letter name) and specifically (by quality and number).',
    @CourseId, 4, NULL, NULL, NULL, GETUTCDATE()
);
SET @Lesson4 = SCOPE_IDENTITY();

INSERT INTO Resources (Title, Description, Type, Url, LessonId, UploadedAt) VALUES
(N'Generic Intervals', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/30', @Lesson4, GETUTCDATE()),
(N'Specific Intervals', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/31', @Lesson4, GETUTCDATE()),
(N'Interval Ear Trainer', N'musictheory.net exercise - practice identifying intervals by ear', 4, N'https://www.musictheory.net/exercises', @Lesson4, GETUTCDATE());

-- ---------------------------------------------------------------------
-- Lesson 5: Introduction to Chords
-- ---------------------------------------------------------------------
DECLARE @Lesson5 INT;
INSERT INTO Lessons (Title, Content, CourseId, OrderIndex, VideoUrl, AudioUrl, NotationUrl, CreatedAt)
VALUES (
    N'Introduction to Chords',
    N'Learn how the four types of triads (major, minor, diminished, augmented) are built, and how they invert.',
    @CourseId, 5, NULL, NULL, NULL, GETUTCDATE()
);
SET @Lesson5 = SCOPE_IDENTITY();

INSERT INTO Resources (Title, Description, Type, Url, LessonId, UploadedAt) VALUES
(N'Introduction to Chords', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/40', @Lesson5, GETUTCDATE()),
(N'Triad Inversion', N'musictheory.net lesson', 4, N'https://www.musictheory.net/lessons/42', @Lesson5, GETUTCDATE()),
(N'Chord Identification Trainer', N'musictheory.net exercise', 4, N'https://www.musictheory.net/exercises', @Lesson5, GETUTCDATE());

-- ---------------------------------------------------------------------
-- Quiz for Lesson 3 (The Major Scale) - Type 0 = Theory
-- ---------------------------------------------------------------------
DECLARE @Quiz3 INT;
INSERT INTO Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
VALUES (N'Major Scale Check', N'Quick check on the major scale formula and key signatures.', 0, @Lesson3, GETUTCDATE(), 1);
SET @Quiz3 = SCOPE_IDENTITY();

INSERT INTO QuizQuestions (QuizId, QuestionText, Type, AudioUrl, ToneSequence, CorrectAnswerIndex, CorrectAnswer, CorrectAnswerText, OptionsJson, Points, OrderIndex) VALUES
(@Quiz3, N'What is the step pattern of a major scale, starting from the root?', 0, NULL, NULL, 2, NULL, NULL, N'["W-W-H-W-W-W-H","W-W-H-W-W-H-W","H-W-W-H-W-W-W","W-H-W-W-H-W-W"]', 10, 1),
(@Quiz3, N'How many sharps are in the key signature of G major?', 0, NULL, NULL, 2, NULL, NULL, N'["0","1","2","3"]', 10, 2),
(@Quiz3, N'The 7th degree of a major scale is called the:', 0, NULL, NULL, 3, NULL, NULL, N'["Subdominant","Submediant","Leading Tone","Mediant"]', 10, 3);

-- ---------------------------------------------------------------------
-- Quiz for Lesson 4 (Intervals) - Type 1 = EarTraining
-- ---------------------------------------------------------------------
DECLARE @Quiz4 INT;
INSERT INTO Quizzes (Title, Description, Type, LessonId, CreatedAt, IsActive)
VALUES (N'Interval Identification', N'Identify intervals by their generic and specific names - including two real, listen-and-answer ear training questions.', 1, @Lesson4, GETUTCDATE(), 1);
SET @Quiz4 = SCOPE_IDENTITY();

INSERT INTO QuizQuestions (QuizId, QuestionText, Type, AudioUrl, ToneSequence, CorrectAnswerIndex, CorrectAnswer, CorrectAnswerText, OptionsJson, Points, OrderIndex) VALUES
(@Quiz4, N'C to G is what generic interval?', 0, NULL, NULL, 3, NULL, NULL, N'["3rd","4th","5th","6th"]', 10, 1),
(@Quiz4, N'C to E (four half steps) is a:', 0, NULL, NULL, 2, NULL, NULL, N'["Minor 3rd","Major 3rd","Perfect 4th","Minor 2nd"]', 10, 2),
(@Quiz4, N'An interval that spans 7 half steps is called a:', 0, NULL, NULL, 1, NULL, NULL, N'["Perfect 5th","Perfect 4th","Major 6th","Tritone"]', 10, 3),
(@Quiz4, N'Listen: two notes will play, root then target. What interval do you hear?', 3, NULL, N'261.63,392.00', NULL, NULL, N'Perfect 5th', NULL, 15, 4),
(@Quiz4, N'Listen: two notes will play, root then target. What interval do you hear?', 3, NULL, N'261.63,329.63', NULL, NULL, N'Major 3rd', NULL, 15, 5),
(@Quiz4, N'Listen to the single note. Which note is it?', 3, NULL, N'261.63', NULL, NULL, N'C', NULL, 10, 6);

PRINT 'Sample course, lessons, quizzes (incl. real tone-based ear training), and musictheory.net resource links created successfully.';
GO
