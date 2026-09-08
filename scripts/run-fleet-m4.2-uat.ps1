[CmdletBinding()]
param(
    [switch]$KeepArtifacts,
    [switch]$DashboardM6Only,
    [int]$BackendPort = 5011,
    [int]$FrontendPort = 5173
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runId = 'uat-' + (Get-Date -Format 'yyyyMMddHHmmss') + '-' + ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$container = "hop-fleet-m42-$runId"
$artifactDir = Join-Path $root "tmp\fleet-m4.2-uat-$runId"
$manifestPath = Join-Path $artifactDir 'fixture.json'
$jsonReport = Join-Path $root 'tmp\fleet-m4.2-uat-results.json'
$markdownReport = Join-Path $root 'tmp\fleet-m4.2-uat-results.md'
$results = [ordered]@{}
$backend = $null
$frontend = $null
$containerStarted = $false
$fixtureCreated = $false
$startedAt = [DateTime]::UtcNow

New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null

function Invoke-Gate([string]$Name, [scriptblock]$Action) {
    $gateStart = [DateTime]::UtcNow
    Write-Host "[M4.2 UAT] START $Name"
    try {
        & $Action
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "$Name exited with $LASTEXITCODE" }
        $script:results[$Name] = [ordered]@{ status = 'Passed'; durationSeconds = [Math]::Round(([DateTime]::UtcNow - $gateStart).TotalSeconds, 2) }
        Write-Host "[M4.2 UAT] PASS  $Name"
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
    $response = Invoke-WebRequest -UseBasicParsing -TimeoutSec 10 -Uri $Url -Headers $Headers
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
        dotnet ef database update --no-build --configuration Release --project $project --startup-project $project
        if ($LASTEXITCODE -ne 0) { throw 'EF database update failed.' }
        dotnet ef migrations list --no-build --configuration Release --project $project --startup-project $project
        if ($LASTEXITCODE -ne 0) { throw 'EF migrations list failed.' }
        dotnet ef migrations script --idempotent --no-build --configuration Release --project $project --startup-project $project --output (Join-Path $artifactDir 'fleet-m4.2-idempotent.sql')
        if ($LASTEXITCODE -ne 0) { throw 'EF idempotent migration generation failed.' }
    }
    Invoke-Gate 'QA Tool Security Guards' {
        $savedEnvironment=$env:ASPNETCORE_ENVIRONMENT;$savedFlag=$env:FLEET_QA_ENABLED
        try {
            $env:ASPNETCORE_ENVIRONMENT='Production';$env:FLEET_QA_ENABLED='true'
            dotnet run --no-build --configuration Release --project (Join-Path $root 'backend\Hop.FleetQaTool\Hop.FleetQaTool.csproj') -- create (Join-Path $artifactDir 'production-denied.json') *> $null
            if($LASTEXITCODE -eq 0){throw 'QA tool accepted Production environment.'}
            $env:ASPNETCORE_ENVIRONMENT='UAT';$env:FLEET_QA_ENABLED='false'
            dotnet run --no-build --configuration Release --project (Join-Path $root 'backend\Hop.FleetQaTool\Hop.FleetQaTool.csproj') -- create (Join-Path $artifactDir 'flag-denied.json') *> $null
            if($LASTEXITCODE -eq 0){throw 'QA tool accepted disabled feature flag.'}
            $global:LASTEXITCODE=0
        } finally {$env:ASPNETCORE_ENVIRONMENT=$savedEnvironment;$env:FLEET_QA_ENABLED=$savedFlag}
    }
    if (-not $DashboardM6Only) { Invoke-Gate 'Backend Regression Tests' {
        $integrationConnection = $env:HOP_E2E_CONNECTION_STRING
        Remove-Item Env:HOP_E2E_CONNECTION_STRING -ErrorAction SilentlyContinue
        try { dotnet test (Join-Path $root 'backend\Hop.Api.Tests\Hop.Api.Tests.csproj') --configuration Release --filter 'Category!=PostgreSqlIntegration&Category!=FleetM42PostgreSqlIntegration' --logger 'trx;LogFileName=backend-regression.trx' --results-directory $artifactDir }
        finally { $env:HOP_E2E_CONNECTION_STRING = $integrationConnection }
    }
    Invoke-Gate 'M4.1 PostgreSQL Regression' { dotnet test (Join-Path $root 'backend\Hop.Api.Tests\Hop.Api.Tests.csproj') --configuration Release --filter 'Category=PostgreSqlIntegration' --logger 'trx;LogFileName=m41-postgres.trx' --results-directory $artifactDir }
    Invoke-Gate 'M4.2 PostgreSQL Integration' { dotnet test (Join-Path $root 'backend\Hop.Api.Tests\Hop.Api.Tests.csproj') --configuration Release --filter 'Category=FleetM42PostgreSqlIntegration' --logger 'trx;LogFileName=m42-postgres.trx' --results-directory $artifactDir }
    Invoke-Gate 'Frontend Lint' { npm --prefix (Join-Path $root 'frontend') run lint }
    Invoke-Gate 'M4.2 Component Tests' { npm --prefix (Join-Path $root 'frontend') run test:unit:fleet-m4.2 }
    Invoke-Gate 'Frontend Coverage Gate' { npm --prefix (Join-Path $root 'frontend') run test:unit:coverage }
    Invoke-Gate 'Frontend Build' { npm --prefix (Join-Path $root 'frontend') run build }
    }

    $env:ASPNETCORE_ENVIRONMENT = 'UAT'
    $env:Database__SeedOnStartup = 'false'
    $env:FLEET_QA_ENABLED = 'true'
    $env:Storage__RootPath = Join-Path $artifactDir 'storage'
    foreach ($role in @('ADMIN', 'DASHBOARD', 'NO_OVERRIDE', 'DRIVER', 'PREVIOUS_DRIVER', 'REQUESTER', 'DISPATCHER', 'REVIEWER', 'DIRECTOR')) {
        Set-Item "Env:FLEET_QA_${role}_PASSWORD" ([Guid]::NewGuid().ToString('N') + '!Aa1')
    }
    Invoke-Gate 'QA Fixture Create' {
        dotnet run --no-build --configuration Release --project (Join-Path $root 'backend\Hop.FleetQaTool\Hop.FleetQaTool.csproj') -- create $manifestPath
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
        $loginResponse = Invoke-WebRequest -UseBasicParsing -TimeoutSec 10 -Method Post -Uri "$baseUrl/api/auth/login" -ContentType 'application/json' -Body $loginBody
        if ($loginResponse.Headers.'Content-Type' -notmatch 'application/json') { throw 'Auth routing smoke did not return JSON.' }
        $login = $loginResponse.Content | ConvertFrom-Json
        $token = $login.data.accessToken
        if ([string]::IsNullOrWhiteSpace($token)) { throw 'Auth routing smoke did not return an access token.' }
        $headers = @{ Authorization="Bearer $token" }
        Assert-JsonResponse "$baseUrl/api/fleet/dashboard/summary?preset=custom&startDate=2026-08-01&endDate=2026-08-02" $headers
        Assert-JsonResponse "$baseUrl/api/fleet/calendar?start=2026-08-01&end=2026-08-02" $headers
        Assert-JsonResponse "$baseUrl/api/fleet/maintenance" $headers
    }

    if (-not $DashboardM6Only) {
    Invoke-Gate 'M4.1 Playwright Regression' { npm --prefix (Join-Path $root 'frontend') run test:e2e:uat }
    Invoke-Gate 'M4.2 Playwright UAT' { npm --prefix (Join-Path $root 'frontend') run test:e2e:fleet-m4.2:uat }
    }
    Invoke-Gate 'Fleet Dashboard M6 Playwright' { npm --prefix (Join-Path $root 'frontend') run test:e2e:fleet-dashboard:m6 }
    if (-not $DashboardM6Only) {
    $playwrightReport = Get-Content -Raw (Join-Path $root 'tmp\fleet-m4.2-playwright.json') | ConvertFrom-Json
    $skipped = [int]$playwrightReport.stats.skipped
    if ($skipped -ne 0 -or [int]$playwrightReport.stats.expected -lt 12 -or [int]$playwrightReport.stats.unexpected -ne 0 -or [int]$playwrightReport.stats.flaky -ne 0) { throw "UAT sign-off rejected: M4.2 Playwright requires >=12 passed, 0 failed/skipped/flaky." }
    }
    $m6Report = Get-Content -Raw (Join-Path $root 'tmp\fleet-dashboard-m6-playwright.json') | ConvertFrom-Json
    if ([int]$m6Report.stats.skipped -ne 0 -or [int]$m6Report.stats.expected -lt 7 -or [int]$m6Report.stats.unexpected -ne 0 -or [int]$m6Report.stats.flaky -ne 0) { throw "M6 sign-off rejected: Fleet Dashboard Playwright requires >=7 passed, 0 failed/skipped/flaky." }
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
            dotnet run --no-build --configuration Release --project (Join-Path $root 'backend\Hop.FleetQaTool\Hop.FleetQaTool.csproj') -- cleanup $manifestPath
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
    $m41PlaywrightCounts = $null
    $playwrightReportPath = Join-Path $root 'tmp\fleet-m4.2-playwright.json'
    if (Test-Path $playwrightReportPath) {
        try {
            $playwrightStats = (Get-Content -Raw $playwrightReportPath | ConvertFrom-Json).stats
            $playwrightCounts = [ordered]@{ passed=[int]$playwrightStats.expected; failed=[int]$playwrightStats.unexpected; skipped=[int]$playwrightStats.skipped; flaky=[int]$playwrightStats.flaky }
        } catch { $playwrightCounts = [ordered]@{ parseError=$_.Exception.Message } }
    }
    $m41ReportPath = Join-Path $root 'tmp\fleet-m4.1-playwright.json'
    if (Test-Path $m41ReportPath) { $s=(Get-Content -Raw $m41ReportPath|ConvertFrom-Json).stats;$m41PlaywrightCounts=[ordered]@{passed=[int]$s.expected;failed=[int]$s.unexpected;skipped=[int]$s.skipped;flaky=[int]$s.flaky} }
    function Read-Trx([string]$Path){if(!(Test-Path $Path)){return $null};[xml]$x=Get-Content -Raw $Path;$c=$x.TestRun.ResultSummary.Counters;return [ordered]@{total=[int]$c.total;passed=[int]$c.passed;failed=[int]$c.failed;skipped=[int]$c.notExecuted}}
    $testCounts=[ordered]@{backendRegression=Read-Trx (Join-Path $artifactDir 'backend-regression.trx');m41PostgreSql=Read-Trx (Join-Path $artifactDir 'm41-postgres.trx');m42PostgreSql=Read-Trx (Join-Path $artifactDir 'm42-postgres.trx');m41Playwright=$m41PlaywrightCounts;m42Playwright=$playwrightCounts}
    $coverage=$null;$coveragePath=Join-Path $root 'frontend\coverage\coverage-summary.json';if(Test-Path $coveragePath){$coverage=(Get-Content -Raw $coveragePath|ConvertFrom-Json).total}
    $summary = [ordered]@{ runId=$runId; startedAt=$startedAt.ToString('O'); finishedAt=[DateTime]::UtcNow.ToString('O'); gitCommit=(git -C $root rev-parse HEAD);worktreeStatus=(git -C $root status --short);migration='20260802090122_CompleteFleetM42Uat';postgresVersion='16';fixedClock='2026-08-01T00:00:00Z';tests=$testCounts;coverage=$coverage;playwright=$playwrightCounts;gates=$results;failure=$failure;verdict=if($failure){'UAT sign-off pending'}else{'Milestone 4.2 UAT-ready'} }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 $jsonReport
    $lines = @('# Fleet Milestone 4.2 UAT Result','',"- Run ID: $runId","- Commit: $($summary.gitCommit)","- Migration: $($summary.migration)","- Started: $($summary.startedAt)","- Finished: $($summary.finishedAt)","- Backend regression: $($testCounts.backendRegression.passed) passed / $($testCounts.backendRegression.failed) failed / $($testCounts.backendRegression.skipped) skipped","- M4.2 PostgreSQL: $($testCounts.m42PostgreSql.passed) passed / $($testCounts.m42PostgreSql.failed) failed / $($testCounts.m42PostgreSql.skipped) skipped","- M4.1 Playwright: $($m41PlaywrightCounts.passed) passed / $($m41PlaywrightCounts.failed) failed / $($m41PlaywrightCounts.skipped) skipped","- M4.2 Playwright: $($playwrightCounts.passed) passed / $($playwrightCounts.failed) failed / $($playwrightCounts.skipped) skipped / $($playwrightCounts.flaky) flaky","- Coverage: statements $($coverage.statements.pct)% / branches $($coverage.branches.pct)% / functions $($coverage.functions.pct)% / lines $($coverage.lines.pct)%",'', '| Gate | Result |','|---|---|')
    foreach ($entry in $results.GetEnumerator()) { $lines += "| $($entry.Key) | $($entry.Value.status) |" }
    if ($failure) { $lines += @('',"**UAT sign-off: FAILED** — $failure") } else { $lines += @('','**UAT sign-off: PASSED**') }
    $lines | Set-Content -Encoding utf8 $markdownReport
    $lines | Set-Content -Encoding utf8 (Join-Path $root 'docs\business-requirements\fleet-milestone-4.2-final-uat.md')
    if (-not $KeepArtifacts -and -not $failure -and (Test-Path $artifactDir)) { Remove-Item -LiteralPath $artifactDir -Recurse -Force }
}

if ($failure) { Write-Error $failure; exit 1 }
Write-Host "UAT passed. Reports: $jsonReport and $markdownReport"
