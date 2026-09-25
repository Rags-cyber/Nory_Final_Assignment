-- =====================================================================
-- Nory Music LMS - Database Creation Script  (SQL Server / LocalDB)
-- Creates the "Nory" database with a schema that matches the current
-- ApplicationDbContext + Models exactly (Identity + Courses + Lessons +
-- Quizzes + QuizQuestions + QuizAttempts + QuizAnswers + Enrollments +
-- Resources).
--
-- HOW TO RUN THIS (SSMS):
--   1. Open SSMS, connect to Server name:  (localdb)\mssqllocaldb
--   2. File > Open > File... and select this script (or paste it into
--      a New Query window).
--   3. Click "Execute" (or press F5).
--
-- NOTE: If you plan to let the app manage the schema with EF Core
-- migrations instead (recommended - see the setup guide), you do NOT
-- need to run this script. Only run this if you want to manually
-- inspect/create the schema in SSMS, or your assignment specifically
-- requires you to show the raw SQL. See the accompanying guide for
-- which path to pick and why EF migrations is the safer of the two.
-- =====================================================================

IF DB_ID('Nory') IS NULL
BEGIN
    CREATE DATABASE [Nory];
END
GO

USE [Nory]
GO

-- =====================================================================
-- IDENTITY TABLES (ASP.NET Core Identity)
-- =====================================================================

CREATE TABLE [dbo].[AspNetRoles] (
    [Id] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(256) NULL,
    [NormalizedName] NVARCHAR(256) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[AspNetUsers] (
    [Id] NVARCHAR(450) NOT NULL,
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(MAX) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL,
    [TwoFactorEnabled] BIT NOT NULL,
    [LockoutEnd] DATETIMEOFFSET(7) NULL,
    [LockoutEnabled] BIT NOT NULL,
    [AccessFailedCount] INT NOT NULL,
    -- ApplicationUser custom properties
    [FirstName] NVARCHAR(MAX) NULL,
    [LastName] NVARCHAR(MAX) NULL,
    [DateOfBirth] DATETIME2(7) NOT NULL,
    [Bio] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] NVARCHAR(128) NOT NULL,
    [ProviderKey] NVARCHAR(128) NOT NULL,
    [ProviderDisplayName] NVARCHAR(MAX) NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] NVARCHAR(450) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId] NVARCHAR(450) NOT NULL,
    [LoginProvider] NVARCHAR(128) NOT NULL,
    [Name] NVARCHAR(128) NOT NULL,
    [Value] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId] ASC, [LoginProvider] ASC, [Name] ASC),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId] ASC)
GO
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName] ASC) WHERE [NormalizedName] IS NOT NULL
GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId] ASC)
GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId] ASC)
GO
CREATE NONCLUSTERED INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail] ASC)
GO
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName] ASC) WHERE [NormalizedUserName] IS NOT NULL
GO

-- =====================================================================
-- APPLICATION TABLES (this is the part that was MISSING before -
-- the app's DbContext referenced these but no migration ever created
-- them, which is why Courses/Lessons/Quizzes screens failed)
-- =====================================================================

CREATE TABLE [dbo].[Courses] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [InstructorId] NVARCHAR(450) NOT NULL,
    [StartDate] DATETIME2(7) NOT NULL,
    [EndDate] DATETIME2(7) NOT NULL,
    [IsActive] BIT NOT NULL,
    [ImageUrl] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_Courses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Courses_AspNetUsers_InstructorId] FOREIGN KEY ([InstructorId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
)
GO

CREATE TABLE [dbo].[Lessons] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [Content] NVARCHAR(2000) NOT NULL,
    [CourseId] INT NOT NULL,
    [OrderIndex] INT NOT NULL,
    [VideoUrl] NVARCHAR(MAX) NULL,
    [AudioUrl] NVARCHAR(MAX) NULL,
    [NotationUrl] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_Lessons] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Lessons_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[Quizzes] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [Type] INT NOT NULL,               -- 0 = Theory, 1 = EarTraining
    [LessonId] INT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [IsActive] BIT NOT NULL,
    CONSTRAINT [PK_Quizzes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Quizzes_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [dbo].[Lessons] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[QuizQuestions] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuizId] INT NOT NULL,
    [QuestionText] NVARCHAR(500) NOT NULL,
    [Type] INT NOT NULL,               -- 0=MultipleChoice 1=TrueFalse 2=FillInBlank 3=AudioIdentification
    [AudioUrl] NVARCHAR(MAX) NULL,
    [CorrectAnswerIndex] INT NULL,
    [CorrectAnswer] BIT NULL,
    [CorrectAnswerText] NVARCHAR(MAX) NULL,
    [OptionsJson] NVARCHAR(MAX) NULL,
    [Points] INT NOT NULL,
    [OrderIndex] INT NOT NULL,
    [ToneSequence] NVARCHAR(200) NULL,  -- comma-separated Hz frequencies for synthesized ear-training audio
    CONSTRAINT [PK_QuizQuestions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_QuizQuestions_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [dbo].[Quizzes] ([Id]) ON DELETE CASCADE
)
GO

CREATE TABLE [dbo].[QuizAttempts] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuizId] INT NOT NULL,
    [StudentId] NVARCHAR(450) NOT NULL,
    [StartedAt] DATETIME2(7) NOT NULL,
    [CompletedAt] DATETIME2(7) NULL,
    [ScorePercentage] FLOAT NULL,
    [IsPassed] BIT NOT NULL,
    CONSTRAINT [PK_QuizAttempts] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [dbo].[Quizzes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QuizAttempts_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
)
GO

CREATE TABLE [dbo].[QuizAnswers] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuizAttemptId] INT NOT NULL,
    [QuizQuestionId] INT NOT NULL,
    [SelectedAnswer] NVARCHAR(MAX) NULL,
    [SelectedAnswerIndex] INT NULL,
    [IsCorrect] BIT NOT NULL,
    [PointsEarned] FLOAT NOT NULL,
    [Feedback] NVARCHAR(MAX) NULL,
    [AnsweredAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_QuizAnswers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_QuizAnswers_QuizAttempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [dbo].[QuizAttempts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuizAnswers_QuizQuestions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [dbo].[QuizQuestions] ([Id]) ON DELETE NO ACTION
)
GO

CREATE TABLE [dbo].[Enrollments] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [StudentId] NVARCHAR(450) NOT NULL,
    [CourseId] INT NOT NULL,
    [EnrolledAt] DATETIME2(7) NOT NULL,
    [CompletedAt] DATETIME2(7) NULL,
    [Status] INT NOT NULL,             -- 0=Active 1=Completed 2=Dropped
    [ProgressPercentage] FLOAT NOT NULL,
    [CertificateUrl] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_Enrollments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Enrollments_AspNetUsers_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Enrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses] ([Id]) ON DELETE NO ACTION
)
GO

-- Educational Resources (new - admin-uploaded materials attached to a lesson)
CREATE TABLE [dbo].[Resources] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Title] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [Type] INT NOT NULL,               -- 0=Pdf 1=SheetMusic 2=AudioTrack 3=Video 4=ExternalLink 5=Other
    [Url] NVARCHAR(1000) NOT NULL,
    [LessonId] INT NOT NULL,
    [UploadedAt] DATETIME2(7) NOT NULL,
    CONSTRAINT [PK_Resources] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Resources_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [dbo].[Lessons] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_Lessons_CourseId] ON [dbo].[Lessons] ([CourseId])
CREATE NONCLUSTERED INDEX [IX_Courses_InstructorId] ON [dbo].[Courses] ([InstructorId])
CREATE NONCLUSTERED INDEX [IX_Quizzes_LessonId] ON [dbo].[Quizzes] ([LessonId])
CREATE NONCLUSTERED INDEX [IX_QuizQuestions_QuizId] ON [dbo].[QuizQuestions] ([QuizId])
CREATE NONCLUSTERED INDEX [IX_QuizAttempts_QuizId] ON [dbo].[QuizAttempts] ([QuizId])
CREATE NONCLUSTERED INDEX [IX_QuizAttempts_StudentId] ON [dbo].[QuizAttempts] ([StudentId])
CREATE NONCLUSTERED INDEX [IX_QuizAnswers_QuizAttemptId] ON [dbo].[QuizAnswers] ([QuizAttemptId])
CREATE NONCLUSTERED INDEX [IX_QuizAnswers_QuizQuestionId] ON [dbo].[QuizAnswers] ([QuizQuestionId])
CREATE NONCLUSTERED INDEX [IX_Enrollments_StudentId] ON [dbo].[Enrollments] ([StudentId])
CREATE NONCLUSTERED INDEX [IX_Enrollments_CourseId] ON [dbo].[Enrollments] ([CourseId])
CREATE NONCLUSTERED INDEX [IX_Resources_LessonId] ON [dbo].[Resources] ([LessonId])
GO

-- =====================================================================
-- SEED: default roles (Admin/Instructor/Student get created by
-- Program.cs on first run too, but this covers the SSMS-only path)
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [NormalizedName] = 'ADMIN')
BEGIN
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES
        (NEWID(), N'Admin', N'ADMIN', NEWID()),
        (NEWID(), N'Instructor', N'INSTRUCTOR', NEWID()),
        (NEWID(), N'Student', N'STUDENT', NEWID())
END
GO

PRINT 'Nory database created successfully.';
GO
