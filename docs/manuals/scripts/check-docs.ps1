[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\DocsCommon.ps1"

$hasError = $false

function Mark-Error {
    param([string]$Message)
    Write-Fail $Message
    $script:hasError = $true
}

try {
    Write-Info "Checking HOP documentation framework..."
    Ensure-ManualDirectories

    if (Test-CommandAvailable "pandoc") {
        Write-Ok "Pandoc found: $((Get-Command pandoc).Source)"
    } else {
        Mark-Error "Pandoc not found. Install with: winget install --id JohnMacFarlane.Pandoc"
    }

    if (Test-LatexAvailable) {
        Write-Ok "LaTeX engine found."
    } else {
        Write-Warn "LaTeX engine not found. PDF build needs MiKTeX/TinyTeX with xelatex. Install with: winget install --id MiKTeX.MiKTeX"
    }

    if (Test-CommandAvailable "wkhtmltopdf") {
        Write-Ok "wkhtmltopdf found."
    } elseif (Test-CommandAvailable "weasyprint") {
        Write-Ok "WeasyPrint found."
    } else {
        Write-Warn "wkhtmltopdf/WeasyPrint not found. They are optional fallback renderers. Install wkhtmltopdf with: winget install --id wkhtmltopdf.wkhtmltox"
    }

    if (Test-CommandAvailable "node") {
        Write-Ok "Node.js found."
    } else {
        Write-Warn "Node.js not found. Mermaid CLI installation needs Node.js. Install with: winget install --id OpenJS.NodeJS.LTS"
    }

    $markdownFiles = Get-Phase1MarkdownFiles
    if ($markdownFiles.Count -gt 0) {
        Write-Ok "Markdown files found: $($markdownFiles.Count)"
    } else {
        Mark-Error "No Markdown files found in $Script:Phase1Dir"
    }

    foreach ($required in @($Script:MetadataFile, $Script:LatexTemplate, $Script:ReferenceDocx)) {
        if (Test-Path $required) {
            Write-Ok "Template exists: $required"
        } else {
            Mark-Error "Required template missing: $required"
        }
    }

    $linkPattern = '(!?)\[[^\]]*\]\(([^)]+)\)'
    $mermaidFound = $false
    foreach ($file in Get-ChildItem -Path $Script:Phase1Dir -Filter "*.md" -File) {
        $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        if ($content -match '```mermaid') {
            $mermaidFound = $true
        }

        foreach ($match in [regex]::Matches($content, $linkPattern)) {
            $isImage = $match.Groups[1].Value -eq "!"
            $target = $match.Groups[2].Value.Trim()
            if ([string]::IsNullOrWhiteSpace($target)) { continue }
            if ($target -match '^(https?:|mailto:|tel:|#)') { continue }

            $pathPart = ($target -split '#')[0]
            if ([string]::IsNullOrWhiteSpace($pathPart)) { continue }
            if ($pathPart -match '^\{.*\}$') { continue }

            $candidate = Join-Path $file.DirectoryName ([System.Uri]::UnescapeDataString($pathPart))
            if (-not (Test-Path $candidate)) {
                if ($isImage) {
                    Write-Warn "Possible broken image path in $($file.Name): $target"
                } else {
                    Write-Warn "Possible broken internal link in $($file.Name): $target"
                }
            }
        }
    }

    if ($mermaidFound) {
        if (Test-CommandAvailable "mmdc") {
            Write-Ok "Mermaid diagrams found and Mermaid CLI is available."
        } else {
            Write-Warn "Mermaid diagrams found but Mermaid CLI (mmdc) is not available. Install with: npm install -g @mermaid-js/mermaid-cli"
        }
    }

    if ($hasError) {
        Write-Fail "Documentation checks finished with errors. Fix the errors above before build-all."
        exit 1
    }

    Write-Ok "Documentation checks completed."
    exit 0
} catch {
    Write-Fail $_.Exception.Message
    exit 1
}
