#Requires -Version 5.1
<#
.SYNOPSIS
    Loads database/seeds/dev_seed.sql into a local ZigZag database.

.DESCRIPTION
    Development data only - five users, two projects, eight tasks spread
    across every status/priority so the Kanban board and dashboard have
    something to render immediately. Idempotent: the seed script wipes its
    own tables before reloading, so this is safe to re-run at any time.
    Never point this at anything but a local/dev database.

.EXAMPLE
    ./seed-dev-data.ps1
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

$seedFile = Join-Path $PSScriptRoot "..\seeds\dev_seed.sql"

$env:PGPASSWORD = $Password
try {
    Write-Host "Seeding development data into '$DatabaseName'..." -ForegroundColor Cyan
    & $psql -U $Username -h $PgHost -p $Port -d $DatabaseName -w -v ON_ERROR_STOP=1 -f $seedFile
    if ($LASTEXITCODE -ne 0) {
        throw "Seeding failed with exit code $LASTEXITCODE."
    }
    Write-Host "Seed data loaded. All seeded users share the password: Passw0rd!" -ForegroundColor Green
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
