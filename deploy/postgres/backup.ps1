param(
    [Parameter(Mandatory = $true)] [string] $HostName,
    [Parameter(Mandatory = $true)] [string] $Database,
    [Parameter(Mandatory = $true)] [string] $Username,
    [Parameter(Mandatory = $true)] [string] $OutputPath,
    [int] $Port = 5432
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:PGPASSWORD)) {
    throw "PGPASSWORD must be supplied by the operator secret environment."
}

$directory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$arguments = @(
    "--host", $HostName,
    "--port", $Port,
    "--username", $Username,
    "--dbname", $Database,
    "--format", "custom",
    "--compress", "9",
    "--no-owner",
    "--no-acl",
    "--file", $OutputPath
)
& pg_dump @arguments
if ($LASTEXITCODE -ne 0) {
    throw "pg_dump failed with exit code $LASTEXITCODE."
}

$hash = Get-FileHash -Algorithm SHA256 -Path $OutputPath
Write-Output "Backup created: $OutputPath"
Write-Output "SHA256: $($hash.Hash)"
