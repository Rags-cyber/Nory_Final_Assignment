-- Run this once against your EXISTING Nory database (created before the
-- ToneSequence column existed). Fresh installs using the updated
-- Nory_Database_Script.sql already include this column and don't need it.

USE [Nory]
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.QuizQuestions') AND name = 'ToneSequence'
)
BEGIN
    ALTER TABLE dbo.QuizQuestions ADD ToneSequence NVARCHAR(200) NULL;
    PRINT 'ToneSequence column added.';
END
ELSE
BEGIN
    PRINT 'ToneSequence column already exists - nothing to do.';
END
GO
