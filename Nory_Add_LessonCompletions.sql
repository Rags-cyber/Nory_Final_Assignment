-- Run once against the existing Nory database to enable accurate,
-- per-student lesson progress tracking.
IF OBJECT_ID(N'dbo.LessonCompletions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonCompletions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LessonCompletions PRIMARY KEY,
        StudentId NVARCHAR(450) NOT NULL,
        LessonId INT NOT NULL,
        CompletedAt DATETIME2(7) NOT NULL,
        CONSTRAINT FK_LessonCompletions_AspNetUsers_StudentId
            FOREIGN KEY (StudentId) REFERENCES dbo.AspNetUsers(Id),
        CONSTRAINT FK_LessonCompletions_Lessons_LessonId
            FOREIGN KEY (LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_LessonCompletions_StudentId_LessonId
        ON dbo.LessonCompletions(StudentId, LessonId);
END;
GO
