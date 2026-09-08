[CmdletBinding()]
param(
    [switch]$KeepArtifacts,
    [int]$BackendPort = 5011,
    [int]$FrontendPort = 5173
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runId = 'uat-' + (Get-Date -Format 'yyyyMMddHHmmss') + '-' + ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$container = "hop-fleet-m41-$runId"
$artifactDir = Join-Path $root "tmp\fleet-m4.1-uat-$runId"
$manifestPath = Join-Path $artifactDir 'fixture.json'
$jsonReport = Join-Path $root 'tmp\fleet-m4.1-uat-results.json'
$markdownReport = Join-Path $root 'tmp\fleet-m4.1-uat-results.md'
$results = [ordered]@{}
$backend = $null
$frontend = $null
$containerStarted = $false
$fixtureCreated = $false
$startedAt = [DateTime]::UtcNow

New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null

function Invoke-Gate([string]$Name, [scriptblock]$Action) {
    $gateStart = [DateTime]::UtcNow
    try {
        & $Action
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "$Name exited with $LASTEXITCODE" }
        $script:results[$Name] = [ordered]@{ status = 'Passed'; durationSeconds = [Math]::Round(([DateTime]::UtcNow - $gateStart).TotalSeconds, 2) }
    } catch {
        $script:results[$Name] = [ordered]@{ status = 'Failed'; durationSeconds = [Math]::Round(([DateTime]::UtcNow - $gateStart).TotalSeconds, 2); error = $_.Exception.Message }
        throw
    }
}

function Wait-Http([string]$Url, [string]$Name) {
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        curl.exe --noproxy '*' --fail --silent --show-error --max-time 2 $Url *> $null
        if ($LASTEXITCODE -eq 0) { return }
        Start-Sleep -Seconds 1
    }
    throw "$Name is unavailable at $Url"
}

function Assert-JsonResponse([string]$Url, [hashtable]$Headers) {
    $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -Headers $Headers
    if ($response.StatusCode -ne 200) { throw "Routing smoke returned $($response.StatusCode) for $Url" }
    if ($response.Headers.'Content-Type' -notmatch 'application/json') { throw "Routing smoke expected JSON but received $($response.Headers.'Content-Type') for $Url" }
    $null = $response.Content | ConvertFrom-Json
}

try {
    Invoke-Gate 'Environment' {
        foreach ($command in @('docker', 'dotnet', 'npm')) {
            if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "$command is required" }
        }
        if ($env:ASPNETCORE_ENVIRONMENT -eq 'Production') { throw 'UAT runner is disabled in Production.' }
        if (docker ps -a --format '{{.Names}}' | Where-Object { $_ -eq $container }) { throw "Refusing to reuse container $container" }
    }

    $dbPassword = [Guid]::NewGuid().ToString('N')
    Invoke-Gate 'PostgreSQL 16' {
        docker run --rm -d --name $container --label hop.test-run=$runId -e POSTGRES_DB=hop_fleet_uat -e POSTGRES_USER=hop_uat -e POSTGRES_PASSWORD=$dbPassword -P postgres:16 | Out-Null
        $script:containerStarted = $true
        for ($attempt = 0; $attempt -lt 45; $attempt++) {
            docker exec $container pg_isready -U hop_uat -d hop_fleet_uat *> $null
            if ($LASTEXITCODE -eq 0) { break }
            Start-Sleep -Seconds 1
        }
        if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL did not become ready.' }
    }
    $dbPort = (docker port $container 5432/tcp).Split(':')[-1]
    $connection = "Host=127.0.0.1;Port=$dbPort;Database=hop_fleet_uat;Username=hop_uat;Password=$dbPassword"
    $env:HOP_E2E_CONNECTION_STRING = $connection
    $env:FLEET_QA_CONNECTION_STRING = $connection
    $env:ConnectionStrings__DefaultConnection = $connection
    $env:FLEET_REQUIRE_POSTGRES_INTEGRATION = 'true'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Jwt__Key = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
    $env:Jwt__Issuer = 'Hop.Fleet.UAT'
    $env:Jwt__Audience = 'Hop.Fleet.UAT.Client'
    $env:Auth__TokenStorageMode = 'LocalStorage'

    Invoke-Gate 'Build' { dotnet build (Join-Path $root 'backend\Hop.Api\Hop.Api.csproj') --configuration Release }
    Invoke-Gate 'Migration Validation' {
        $project = Join-Path $root 'backend\Hop.Api\Hop.Api.csproj'
        dotnet ef database update --project $project --startup-project $project
        if ($LASTEXITCODE -ne 0) { throw 'EF database update failed.' }
        dotnet ef migrations list --project $project --startup-project $project
        if ($LASTEXITCODE -ne 0) { throw 'EF migrations list failed.' }
        dotnet ef migrations script --idempotent --project $project --startup-project $project --output (Join-Path $artifactDir 'fleet-m4.1-idempotent.sql')
        if ($LASTEXITCODE -ne 0) { throw 'EF idempotent migration generation failed.' }
    }
    Invoke-Gate 'Backend Unit Tests' {
        $integrationConnection = $env:HOP_E2E_CONNECTION_STRING
        Remove-Item Env:HOP_E2E_CONNECTION_STRING -ErrorAction SilentlyContinue
        try { dotnet test (Join-Path $root 'backend\Hop.Api.Tests\Hop.Api.Tests.csproj') --configuration Release --filter 'Category!=PostgreSqlIntegration' }
        finally { $env:HOP_E2E_CONNECTION_STRING = $integrationConnection }
    }
    Invoke-Gate 'PostgreSQL Integration Tests' { dotnet test (Join-Path $root 'backend\Hop.Api.Tests\Hop.Api.Tests.csproj') --configuration Release --filter 'Category=PostgreSqlIntegration' }
    Invoke-Gate 'Frontend Lint' { npm --prefix (Join-Path $root 'frontend') run lint }
    Invoke-Gate 'Frontend Component Tests' { npm --prefix (Join-Path $root 'frontend') run test:unit:coverage }
    Invoke-Gate 'Frontend Build' { npm --prefix (Join-Path $root 'frontend') run build }

    $env:ASPNETCORE_ENVIRONMENT = 'UAT'
    $env:Database__SeedOnStartup = 'false'
    foreach ($role in @('ADMIN', 'DASHBOARD', 'NO_OVERRIDE', 'DRIVER')) {
        Set-Item "Env:FLEET_QA_${role}_PASSWORD" ([Guid]::NewGuid().ToString('N') + '!Aa1')
    }
    Invoke-Gate 'QA Fixture Create' {
        dotnet run --project (Join-Path $root 'backend\Hop.FleetQaTool\Hop.FleetQaTool.csproj') -- create $manifestPath
        $script:fixtureCreated = $true
    }
    $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
    $env:FLEET_QA_FIXTURE_PATH = $manifestPath
    $env:FLEET_UAT_MODE = 'true'
    $env:FLEET_QA_BASE_URL = "http://127.0.0.1:$FrontendPort"

    Invoke-Gate 'Services' {
        $backendLog = Join-Path $artifactDir 'backend.log'
        $frontendLog = Join-Path $artifactDir 'frontend.log'
        $backendDll = Join-Path $root 'backend\Hop.Api\bin\Release\net9.0\Hop.Api.dll'
        $script:backend = Start-Process dotnet -ArgumentList @($backendDll,'--urls',"http://127.0.0.1:$BackendPort") -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput $backendLog -RedirectStandardError (Join-Path $artifactDir 'backend.err.log') -PassThru
        $env:VITE_DEV_PROXY_TARGET = "http://127.0.0.1:$BackendPort"
        $env:VITE_API_URL = "http://127.0.0.1:$FrontendPort"
        $env:VITE_API_BASE_URL = "http://127.0.0.1:$FrontendPort"
        $env:VITE_AUTH_TOKEN_STORAGE_MODE = 'localstorage'
        $viteEntry = Join-Path $root 'frontend\node_modules\vite\bin\vite.js'
        $script:frontend = Start-Process node -ArgumentList @($viteEntry,'--host','127.0.0.1','--port',"$FrontendPort") -WorkingDirectory (Join-Path $root 'frontend') -WindowStyle Hidden -RedirectStandardOutput $frontendLog -RedirectStandardError (Join-Path $artifactDir 'frontend.err.log') -PassThru
        Wait-Http "http://127.0.0.1:$BackendPort/health" 'Backend'
        Wait-Http "http://127.0.0.1:$FrontendPort" 'Frontend'
    }

    Invoke-Gate 'Routing Smoke' {
        $baseUrl = "http://127.0.0.1:$FrontendPort"
        $loginBody = @{ username=$manifest.users.dashboard.username; password=$env:FLEET_QA_DASHBOARD_PASSWORD } | ConvertTo-Json
        $loginResponse = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseUrl/api/auth/login" -ContentType 'application/json' -Body $loginBody
        if ($loginResponse.Headers.'Content-Type' -notmatch 'application/json') { throw 'Auth routing smoke did not return JSON.' }
        $login = $loginResponse.Content | ConvertFrom-Json
        $token = $login.data.accessToken
        if ([string]::IsNullOrWhiteSpace($token)) { throw 'Auth routing smoke did not return an access token.' }
        $headers = @{ Authorization="Bearer $token" }
        Assert-JsonResponse "$baseUrl/api/fleet/dashboard/summary?preset=custom&startDate=2026-08-01&endDate=2026-08-02" $headers
        Assert-JsonResponse "$baseUrl/api/fleet/calendar?start=2026-08-01&end=2026-08-02" $headers
        Assert-JsonResponse "$baseUrl/api/fleet/maintenance" $headers
    }

    Invoke-Gate 'Playwright UAT' { npm --prefix (Join-Path $root 'frontend') run test:e2e:uat }
    $playwrightReport = Get-Content -Raw (Join-Path $root 'tmp\fleet-m4.1-playwright.json') | ConvertFrom-Json
    $skipped = [int]$playwrightReport.stats.skipped
    if ($skipped -ne 0) { throw "UAT sign-off rejected: Playwright skipped $skipped test(s)." }
    Invoke-Gate 'Git Diff Check' { git -C $root diff --check }
}
catch {
    $failure = $_.Exception.Message
}
finally {
    try {
        foreach ($process in @($frontend, $backend)) { if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id -Force -ErrorAction Stop } }
        $results['Process Cleanup'] = @{ status = 'Passed' }
    } catch { $results['Process Cleanup'] = @{ status = 'Failed'; error = $_.Exception.Message }; $failure = $failure ?? $_.Exception.Message }
    Start-Sleep -Milliseconds 500
    if ($fixtureCreated) {
        try {
            dotnet run --project (Join-Path $root 'backend\Hop.FleetQaTool\Hop.FleetQaTool.csproj') -- cleanup $manifestPath
            if ($LASTEXITCODE -ne 0) { throw "Cleanup exited with $LASTEXITCODE" }
            $results['Cleanup'] = @{ status = 'Passed' }
        } catch { $results['Cleanup'] = @{ status = 'Failed'; error = $_.Exception.Message }; $failure = $failure ?? $_.Exception.Message }
    }
    if ($containerStarted) {
        try {
            $label = docker inspect --format '{{index .Config.Labels "hop.test-run"}}' $container
            if ($label -ne $runId) { throw 'Container ownership validation failed.' }
            docker stop $container | Out-Null
            $results['Container Cleanup'] = @{ status = 'Passed' }
        } catch { $results['Container Cleanup'] = @{ status = 'Failed'; error = $_.Exception.Message }; $failure = $failure ?? $_.Exception.Message }
    }
    $playwrightCounts = $null
    $playwrightReportPath = Join-Path $root 'tmp\fleet-m4.1-playwright.json'
    if (Test-Path $playwrightReportPath) {
        try {
            $playwrightStats = (Get-Content -Raw $playwrightReportPath | ConvertFrom-Json).stats
            $playwrightCounts = [ordered]@{ passed=[int]$playwrightStats.expected; failed=[int]$playwrightStats.unexpected; skipped=[int]$playwrightStats.skipped; flaky=[int]$playwrightStats.flaky }
        } catch { $playwrightCounts = [ordered]@{ parseError=$_.Exception.Message } }
    }
    $summary = [ordered]@{ runId=$runId; startedAt=$startedAt.ToString('O'); finishedAt=[DateTime]::UtcNow.ToString('O'); gitCommit=(git -C $root rev-parse HEAD); postgresVersion='16'; fixedClock='2026-08-01T00:00:00Z'; playwright=$playwrightCounts; gates=$results; failure=$failure }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 $jsonReport
    $lines = @('# Fleet Milestone 4.1 UAT Result','',"- Run ID: $runId","- Started: $($summary.startedAt)","- Finished: $($summary.finishedAt)","- Playwright: $($playwrightCounts.passed) passed / $($playwrightCounts.failed) failed / $($playwrightCounts.skipped) skipped / $($playwrightCounts.flaky) flaky",'', '| Gate | Result |','|---|---|')
    foreach ($entry in $results.GetEnumerator()) { $lines += "| $($entry.Key) | $($entry.Value.status) |" }
    if ($failure) { $lines += @('',"**UAT sign-off: FAILED** — $failure") } else { $lines += @('','**UAT sign-off: PASSED**') }
    $lines | Set-Content -Encoding utf8 $markdownReport
    if (-not $KeepArtifacts -and -not $failure -and (Test-Path $artifactDir)) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
}

if ($failure) { Write-Error $failure; exit 1 }
Write-Host "UAT passed. Reports: $jsonReport and $markdownReport"
