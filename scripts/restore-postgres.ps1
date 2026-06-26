param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$EnvFile = ".env.production",
    [string]$ComposeFile = "docker-compose.prod.yml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) {
    throw "Backup file not found: $BackupFile"
}

$fileName = Split-Path $BackupFile -Leaf
$targetPath = Join-Path "database/backup" $fileName

if ((Resolve-Path $BackupFile).Path -ne (Resolve-Path $targetPath -ErrorAction SilentlyContinue).Path) {
    Copy-Item -LiteralPath $BackupFile -Destination $targetPath -Force
}

docker compose --env-file $EnvFile -f $ComposeFile up -d postgres
$containerCommand = "pg_restore -U `"`$POSTGRES_USER`" -d `"`$POSTGRES_DB`" --clean --if-exists `"/backup/$fileName`""
docker compose --env-file $EnvFile -f $ComposeFile exec -T postgres sh -c $containerCommand

Write-Host "Restore completed from: $targetPath"
