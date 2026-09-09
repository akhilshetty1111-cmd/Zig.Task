#Requires -Version 5.1
<#
.SYNOPSIS
    Applies every SQL script in database/migrations, in filename order, to a
    ZigZag PostgreSQL database.

.DESCRIPTION
    Migrations are forward-only and never edited once merged (see
    docs/database.md) - this script simply runs each one through psql inside
    its own transaction and stops on the first error. It does not track which
    migrations have already been applied; for the single-migration state of
    Phase 2 that tracking would be premature. A migrations-ledger table is a
    natural Phase-6+ addition once the schema starts changing across commits.

.PARAMETER PgBinPath
    Directory containing psql.exe. Defaults to the standard Windows install
    location for PostgreSQL 17.

.EXAMPLE
    ./apply-migrations.ps1
    Applies all migrations to localhost:5432/zigzag as the zigzag role.

.EXAMPLE
    ./apply-migrations.ps1 -DatabaseName zigzag_test -Port 5433
#>
param(
    [string]$PgHost = "127.0.0.1",
    [int]$Port = 5432,
    [string]$DatabaseName = "zigzag",
    [string]$Username = "zigzag",
    [string]$Password = "zigzag",
    [string]$PgBinPath = "C:\Program Files\PostgreSQL\17\bin"
)

$ErrorActionPreference = "Stop"

$psql = Join-Path $PgBinPath "psql.exe"
if (-not (Test-Path $psql)) {
    throw "psql.exe not found at '$psql'. Pass -PgBinPath, or install PostgreSQL."
}

$migrationsDir = Join-Path $PSScriptRoot "..\migrations"
$migrations = Get-ChildItem -Path $migrationsDir -Filter "*.sql" | Sort-Object Name

if ($migrations.Count -eq 0) {
    Write-Host "No migrations found in $migrationsDir"
    exit 0
}

$env:PGPASSWORD = $Password
try {
    foreach ($migration in $migrations) {
        Write-Host "Applying $($migration.Name)..." -ForegroundColor Cyan
        & $psql -U $Username -h $PgHost -p $Port -d $DatabaseName -w -v ON_ERROR_STOP=1 -f $migration.FullName
        if ($LASTEXITCODE -ne 0) {
            throw "Migration '$($migration.Name)' failed with exit code $LASTEXITCODE."
        }
    }
    Write-Host "All $($migrations.Count) migration(s) applied successfully." -ForegroundColor Green
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
