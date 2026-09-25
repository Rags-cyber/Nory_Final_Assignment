@echo off
REM Delete Nory database from LocalDB

echo Dropping Nory database from LocalDB...
sqlcmd -S (localdb)\mssqllocaldb -Q "ALTER DATABASE [Nory] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Nory];" 2>nul

if %ERRORLEVEL% EQU 0 (
	echo Database dropped successfully
) else (
	echo Database does not exist or error occurred (this is OK)
)

echo.
echo You can now run the application and it will recreate the database with the correct schema.
pause
