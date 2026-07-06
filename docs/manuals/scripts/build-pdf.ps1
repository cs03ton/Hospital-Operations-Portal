[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\DocsCommon.ps1"

try {
    Ensure-ManualDirectories
    $pandoc = Get-PandocPath
    $engine = Get-LatexEngine
    Build-CombinedMarkdown

    $output = Join-Path $Script:PdfDir "HOP-Phase1-User-Manual.pdf"
    $args = @(
        $Script:CombinedFile,
        "--from=markdown+tex_math_dollars+pipe_tables+task_lists",
        "--metadata-file=$Script:MetadataFile",
        "--template=$Script:LatexTemplate",
        "--pdf-engine=$engine",
        "--toc",
        "--toc-depth=3",
        "--standalone",
        "--output=$output"
    )
    Invoke-ExternalCommand -FilePath $pandoc -Arguments $args -ErrorMessage "Pandoc PDF build failed."
    Write-Ok "PDF written: $output"

    if (Test-Path $Script:QuickStartFile) {
        $quickOutput = Join-Path $Script:PdfDir "HOP-Phase1-QuickStart.pdf"
        $quickArgs = @(
            $Script:QuickStartFile,
            "--from=markdown+pipe_tables+task_lists",
            "--metadata-file=$Script:MetadataFile",
            "--template=$Script:LatexTemplate",
            "--pdf-engine=$engine",
            "--standalone",
            "--output=$quickOutput"
        )
        Invoke-ExternalCommand -FilePath $pandoc -Arguments $quickArgs -ErrorMessage "Pandoc QuickStart PDF build failed."
        Write-Ok "QuickStart PDF written: $quickOutput"
    }

    exit 0
} catch {
    Write-Fail $_.Exception.Message
    Write-Warn "PDF build requires Pandoc and MiKTeX/TinyTeX with Thai fonts installed."
    exit 1
}
