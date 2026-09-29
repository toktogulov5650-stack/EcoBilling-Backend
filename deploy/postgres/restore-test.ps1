param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [Parameter(Mandatory = $true)] [string] $TargetDatabase,
    [Parameter(Mandatory = $true)] [string] $Username,
    [Parameter(Mandatory = $true)] [string] $BackupPath,
    [int] $Port = 5432
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:PGPASSWORD)) {
    throw "PGPASSWORD must be supplied by the operator secret environment."
}

if (-not (Test-Path -LiteralPath $BackupPath)) {
    throw "Backup file was not found: $BackupPath"
}

& dropdb --host $HostName --port $Port --username $Username --if-exists $TargetDatabase
if ($LASTEXITCODE -ne 0) { throw "dropdb failed with exit code $LASTEXITCODE." }

& createdb --host $HostName --port $Port --username $Username $TargetDatabase
if ($LASTEXITCODE -ne 0) { throw "createdb failed with exit code $LASTEXITCODE." }

& pg_restore --host $HostName --port $Port --username $Username --dbname $TargetDatabase --no-owner --no-acl --exit-on-error $BackupPath
if ($LASTEXITCODE -ne 0) { throw "pg_restore failed with exit code $LASTEXITCODE." }

$migrationCount = & psql --host $HostName --port $Port --username $Username --dbname $TargetDatabase --tuples-only --no-align --command 'SELECT count(*) FROM "__EFMigrationsHistory";'
if ($LASTEXITCODE -ne 0) { throw "Restore validation query failed with exit code $LASTEXITCODE." }

Write-Output "Restore test completed for database '$TargetDatabase'."
Write-Output "Applied migration records: $($migrationCount.Trim())"
Write-Output "Run application smoke/read-only validation before treating the restore drill as production evidence."
