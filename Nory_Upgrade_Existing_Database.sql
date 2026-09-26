/*
    Upgrade an existing Nory database to the schema used by the current app.

    Run this script in SSMS against the Nory database. It is safe to re-run:
    existing columns/tables/indexes are left in place.

    Includes:
      - QuizQuestions.ToneSequence for synthesized ear-training audio
      - Resources for lesson learning materials
      - LessonCompletions for accurate per-student progress tracking
      - StudentAwards for quiz/course achievement awards
      - ChordSongs for the in-app chord/song library
*/
USE [Nory];
GO

IF OBJECT_ID(N'dbo.QuizQuestions', N'U') IS NULL
    THROW 51000, 'Nory.QuizQuestions is missing. Run Nory_Database_Script.sql first.', 1;
IF OBJECT_ID(N'dbo.Lessons', N'U') IS NULL
    THROW 51000, 'Nory.Lessons is missing. Run Nory_Database_Script.sql first.', 1;
IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
    THROW 51000, 'Nory.AspNetUsers is missing. Run Nory_Database_Script.sql first.', 1;
GO

-- Profile fields are stored on the Identity user row and are shared by the
-- common profile plus the role-specific Student/Instructor/Admin sections.
IF COL_LENGTH(N'dbo.AspNetUsers', N'Instrument') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD Instrument NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'SkillLevel') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD SkillLevel NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'LearningGoals') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD LearningGoals NVARCHAR(1000) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'TeachingSpecialty') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD TeachingSpecialty NVARCHAR(150) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'YearsTeaching') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD YearsTeaching INT NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'AdminDepartment') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD AdminDepartment NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'AdminJobTitle') IS NULL
    ALTER TABLE dbo.AspNetUsers ADD AdminJobTitle NVARCHAR(100) NULL;
GO

-- Ear-training questions can play browser-synthesized tone sequences.
IF COL_LENGTH(N'dbo.QuizQuestions', N'ToneSequence') IS NULL
BEGIN
    ALTER TABLE dbo.QuizQuestions ADD ToneSequence NVARCHAR(200) NULL;
    PRINT 'Added QuizQuestions.ToneSequence.';
END
ELSE
    PRINT 'QuizQuestions.ToneSequence already exists.';
GO

-- Instructor/admin-managed materials attached to lessons.
IF OBJECT_ID(N'dbo.Resources', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resources
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Resources PRIMARY KEY CLUSTERED,
        Title NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        Type INT NOT NULL CONSTRAINT DF_Resources_Type DEFAULT (5),
        Url NVARCHAR(1000) NOT NULL,
        LessonId INT NOT NULL,
        UploadedAt DATETIME2(7) NOT NULL
            CONSTRAINT DF_Resources_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Resources_Lessons_LessonId
            FOREIGN KEY (LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE
    );
    PRINT 'Created dbo.Resources.';
END
ELSE
    PRINT 'dbo.Resources already exists.';
GO

-- Type values must match the ResourceType enum in Models/Resource.cs:
-- 0 Pdf, 1 SheetMusic, 2 AudioTrack, 3 Video, 4 ExternalLink, 5 Other.

-- One completion row per student and lesson; progress is calculated from
-- these records rather than inferred from lesson ordering.
IF OBJECT_ID(N'dbo.LessonCompletions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonCompletions
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_LessonCompletions PRIMARY KEY CLUSTERED,
        StudentId NVARCHAR(450) NOT NULL,
        LessonId INT NOT NULL,
        CompletedAt DATETIME2(7) NOT NULL
            CONSTRAINT DF_LessonCompletions_CompletedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_LessonCompletions_AspNetUsers_StudentId
            FOREIGN KEY (StudentId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_LessonCompletions_Lessons_LessonId
            FOREIGN KEY (LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_LessonCompletions_StudentId_LessonId
        ON dbo.LessonCompletions(StudentId, LessonId);
    PRINT 'Created dbo.LessonCompletions and its unique student/lesson index.';
END
ELSE
    PRINT 'dbo.LessonCompletions already exists.';
GO

-- Student rewards/badges; awards are granted once per student and award key.
IF OBJECT_ID(N'dbo.StudentAwards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StudentAwards
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_StudentAwards PRIMARY KEY CLUSTERED,
        StudentId NVARCHAR(450) NOT NULL,
        AwardKey NVARCHAR(120) NOT NULL,
        Title NVARCHAR(120) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        EarnedAt DATETIME2(7) NOT NULL
            CONSTRAINT DF_StudentAwards_EarnedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StudentAwards_AspNetUsers_StudentId
            FOREIGN KEY (StudentId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_StudentAwards_StudentId_AwardKey
        ON dbo.StudentAwards(StudentId, AwardKey);
    PRINT 'Created dbo.StudentAwards and its unique student/award index.';
END
ELSE
    PRINT 'dbo.StudentAwards already exists.';
GO

-- In-app song chord guides, instrument and practice-tempo metadata.
IF OBJECT_ID(N'dbo.ChordSongs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChordSongs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChordSongs PRIMARY KEY CLUSTERED,
        Title NVARCHAR(150) NOT NULL,
        Artist NVARCHAR(150) NOT NULL,
        Instrument NVARCHAR(40) NOT NULL,
        [Key] NVARCHAR(20) NULL,
        CapoFret INT NULL,
        TempoBpm INT NULL,
        TempoNote NVARCHAR(80) NULL,
        ChordMap NVARCHAR(2000) NOT NULL,
        StrummingPattern NVARCHAR(250) NULL,
        AttributionUrl NVARCHAR(1000) NULL,
        AddedAt DATETIME2(7) NOT NULL CONSTRAINT DF_ChordSongs_AddedAt DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.ChordSongs.';
END
ELSE
    PRINT 'dbo.ChordSongs already exists.';
GO
