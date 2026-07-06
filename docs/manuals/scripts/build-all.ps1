[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

try {
    Write-Host "[INFO] Running documentation checks..." -ForegroundColor Cyan
    & "$PSScriptRoot\check-docs.ps1"
    if ($LASTEXITCODE -ne 0) {
        throw "check-docs.ps1 failed. Build stopped."
    }

    & "$PSScriptRoot\build-pdf.ps1"
    if ($LASTEXITCODE -ne 0) { throw "PDF build failed." }

    & "$PSScriptRoot\build-docx.ps1"
    if ($LASTEXITCODE -ne 0) { throw "DOCX build failed." }

    & "$PSScriptRoot\build-html.ps1"
    if ($LASTEXITCODE -ne 0) { throw "HTML build failed." }

    Write-Host "[OK] Build complete." -ForegroundColor Green
    exit 0
} catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
