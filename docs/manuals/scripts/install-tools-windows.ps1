[CmdletBinding()]
param(
    [switch]$InstallMermaidCli
)

$ErrorActionPreference = "Continue"

function Write-Step {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Name
    )

    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        Write-Host "[WARN] winget not found. Please install $Name manually." -ForegroundColor Yellow
        return
    }

    Write-Step "Installing $Name with winget..."
    winget install --id $Id --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] $Name installation command completed." -ForegroundColor Green
    } else {
        Write-Host "[WARN] $Name installation may not have completed. Check winget output above." -ForegroundColor Yellow
    }
}

Write-Step "Preparing documentation toolchain for HOP manuals..."

Install-WingetPackage -Id "JohnMacFarlane.Pandoc" -Name "Pandoc"
Install-WingetPackage -Id "MiKTeX.MiKTeX" -Name "MiKTeX"
Install-WingetPackage -Id "wkhtmltopdf.wkhtmltox" -Name "wkhtmltopdf"
Install-WingetPackage -Id "OpenJS.NodeJS.LTS" -Name "Node.js LTS"

if ($InstallMermaidCli) {
    if (Get-Command npm -ErrorAction SilentlyContinue) {
        Write-Step "Installing Mermaid CLI with npm..."
        npm install -g @mermaid-js/mermaid-cli
    } else {
        Write-Host "[WARN] npm not found. Install Node.js first, then run: npm install -g @mermaid-js/mermaid-cli" -ForegroundColor Yellow
    }
} else {
    Write-Host "[INFO] Mermaid CLI is optional. To install it, rerun with -InstallMermaidCli or use: npm install -g @mermaid-js/mermaid-cli" -ForegroundColor Cyan
}

Write-Host "[INFO] Recommended browser fallback: Google Chrome or Microsoft Edge." -ForegroundColor Cyan
Write-Host "[INFO] After installation, open a new PowerShell window and run: .\docs\manuals\scripts\check-docs.ps1" -ForegroundColor Cyan
