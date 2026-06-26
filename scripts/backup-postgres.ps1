param(
    [string]$EnvFile = ".env.production",
    [string]$ComposeFile = "docker-compose.prod.yml",
    [string]$OutputDirectory = "database/backup"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $EnvFile)) {
    throw "Environment file not found: $EnvFile"
}

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outputPath = Join-Path $OutputDirectory "hop_db_$timestamp.dump"
$fileName = Split-Path $outputPath -Leaf
$containerCommand = "pg_dump -U `"`$POSTGRES_USER`" -d `"`$POSTGRES_DB`" -Fc --file `"/backup/$fileName`""

docker compose --env-file $EnvFile -f $ComposeFile exec -T postgres sh -c $containerCommand

Write-Host "Backup created: $outputPath"
