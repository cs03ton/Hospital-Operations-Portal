[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\DocsCommon.ps1"

try {
    Ensure-ManualDirectories
    $pandoc = Get-PandocPath
    if (-not (Test-Path $Script:ReferenceDocx)) {
        throw "Reference DOCX not found: $Script:ReferenceDocx"
    }
    Build-CombinedMarkdown

    $output = Join-Path $Script:DocxDir "HOP-Phase1-User-Manual.docx"
    $args = @(
        $Script:CombinedFile,
        "--from=markdown+pipe_tables+task_lists",
        "--metadata-file=$Script:MetadataFile",
        "--reference-doc=$Script:ReferenceDocx",
        "--toc",
        "--toc-depth=3",
        "--standalone",
        "--output=$output"
    )
    Invoke-ExternalCommand -FilePath $pandoc -Arguments $args -ErrorMessage "Pandoc DOCX build failed."
    Write-Ok "DOCX written: $output"

    if (Test-Path $Script:QuickStartFile) {
        $quickOutput = Join-Path $Script:DocxDir "HOP-Phase1-QuickStart.docx"
        $quickArgs = @(
            $Script:QuickStartFile,
            "--from=markdown+pipe_tables+task_lists",
            "--metadata-file=$Script:MetadataFile",
            "--reference-doc=$Script:ReferenceDocx",
            "--standalone",
            "--output=$quickOutput"
        )
        Invoke-ExternalCommand -FilePath $pandoc -Arguments $quickArgs -ErrorMessage "Pandoc QuickStart DOCX build failed."
        Write-Ok "QuickStart DOCX written: $quickOutput"
    }

    exit 0
} catch {
    Write-Fail $_.Exception.Message
    Write-Warn "DOCX build requires Pandoc and a valid reference DOCX."
    exit 1
}
