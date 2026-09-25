param(
	[string]$instanceName = "(localdb)\mssqllocaldb",
	[string]$databaseName = "Nory"
)

Write-Host "Attempting to drop database '$databaseName' from $instanceName..." -ForegroundColor Cyan

try {
	$connectionString = "Server=$instanceName;Database=master;Trusted_Connection=True;"
	$connection = New-Object Microsoft.Data.SqlClient.SqlConnection
	$connection.ConnectionString = $connectionString
	$connection.Open()

	Write-Host "Connected to LocalDB master database" -ForegroundColor Green

	# First, set the database to single-user mode to drop active connections
	$command = $connection.CreateCommand()
	$command.CommandTimeout = 30
	$command.CommandText = "ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;"

	try {
		$command.ExecuteNonQuery() | Out-Null
		Write-Host "Set database to SINGLE_USER mode" -ForegroundColor Green
	} catch {
		Write-Host "Database might not exist (this is OK)" -ForegroundColor Yellow
	}

	# Drop the database
	$command.CommandText = "DROP DATABASE IF EXISTS [$databaseName];"
	$command.ExecuteNonQuery() | Out-Null

	Write-Host "Database '$databaseName' dropped successfully!" -ForegroundColor Green
	Write-Host "The database will be recreated with correct schema on the next application run." -ForegroundColor Green

	$connection.Close()
}
catch {
	Write-Host "Error: $_" -ForegroundColor Red
	Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
	Write-Host "1. Make sure Visual Studio is not debugging the application"
	Write-Host "2. Close any SQL Server Management Studio windows"
	Write-Host "3. Try running: sqlcmd -S (localdb)\mssqllocaldb -Q `"DROP DATABASE [Nory];`""
}
