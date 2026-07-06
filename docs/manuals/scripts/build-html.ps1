[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\DocsCommon.ps1"

try {
    Ensure-ManualDirectories
    $pandoc = Get-PandocPath
    Build-CombinedMarkdown

    $css = Join-Path $Script:HtmlDir "hop-manual.css"
    $cssContent = @"
:root {
  --hop-primary: #0F766E;
  --hop-secondary: #14B8A6;
  --hop-accent: #DCFCE7;
  --hop-text: #374151;
  --hop-muted: #6B7280;
  --hop-border: #E5E7EB;
  --hop-bg: #FFFFFF;
  --hop-code: #F3F4F6;
}
* { box-sizing: border-box; }
html { scroll-behavior: smooth; }
body {
  margin: 0;
  color: var(--hop-text);
  background: var(--hop-bg);
  font-family: "Segoe UI", "TH Sarabun New", "Noto Sans Thai", Tahoma, sans-serif;
  font-size: 18px;
  line-height: 1.72;
}
a { color: var(--hop-primary); text-decoration: none; }
a:hover { text-decoration: underline; }
header {
  position: sticky;
  top: 0;
  z-index: 3;
  border-bottom: 1px solid var(--hop-border);
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(10px);
}
.topbar {
  max-width: 1440px;
  margin: 0 auto;
  padding: 12px 24px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}
.brand { font-weight: 700; color: var(--hop-primary); }
.meta { color: var(--hop-muted); font-size: 14px; }
.layout {
  max-width: 1440px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 320px minmax(0, 1fr);
  gap: 32px;
  padding: 24px;
}
nav#TOC {
  position: sticky;
  top: 72px;
  align-self: start;
  max-height: calc(100vh - 96px);
  overflow: auto;
  border-right: 1px solid var(--hop-border);
  padding-right: 20px;
  font-size: 15px;
}
nav#TOC ul { list-style: none; padding-left: 0; margin: 0; }
nav#TOC li { margin: 6px 0; }
nav#TOC ul ul { padding-left: 16px; border-left: 2px solid var(--hop-accent); }
main {
  min-width: 0;
  max-width: 920px;
  padding-bottom: 64px;
}
h1, h2, h3, h4 { line-height: 1.25; letter-spacing: 0; }
h1 { color: var(--hop-primary); font-size: 34px; margin: 8px 0 24px; }
h2 { color: var(--hop-primary); font-size: 28px; margin-top: 40px; border-bottom: 1px solid var(--hop-border); padding-bottom: 6px; }
h3 { color: var(--hop-secondary); font-size: 24px; margin-top: 28px; }
h4 { font-size: 20px; margin-top: 22px; }
table { width: 100%; border-collapse: collapse; margin: 18px 0; font-size: 16px; }
th, td { border: 1px solid var(--hop-border); padding: 8px 10px; vertical-align: top; }
th { background: var(--hop-accent); color: #064E3B; text-align: left; }
blockquote {
  margin: 18px 0;
  padding: 12px 16px;
  border-left: 4px solid var(--hop-secondary);
  background: #F0FDFA;
}
code { background: var(--hop-code); padding: 2px 5px; border-radius: 4px; }
pre { background: var(--hop-code); padding: 14px; overflow: auto; border-radius: 6px; }
img { max-width: 100%; height: auto; border: 1px solid var(--hop-border); border-radius: 6px; }
.page-break { break-before: page; }
@media (max-width: 960px) {
  .layout { grid-template-columns: 1fr; padding: 18px; }
  nav#TOC { position: static; max-height: none; border-right: 0; border-bottom: 1px solid var(--hop-border); padding: 0 0 18px; }
  .topbar { align-items: flex-start; flex-direction: column; }
  body { font-size: 17px; }
  h1 { font-size: 30px; }
  h2 { font-size: 24px; }
  h3 { font-size: 21px; }
}
"@
    [System.IO.File]::WriteAllText($css, $cssContent, [System.Text.UTF8Encoding]::new($false))

    $output = Join-Path $Script:HtmlDir "index.html"
    $args = @(
        $Script:CombinedFile,
        "--from=markdown+pipe_tables+task_lists",
        "--metadata-file=$Script:MetadataFile",
        "--standalone",
        "--section-divs",
        "--toc",
        "--toc-depth=3",
        "--css=hop-manual.css",
        "--output=$output"
    )
    Invoke-ExternalCommand -FilePath $pandoc -Arguments $args -ErrorMessage "Pandoc HTML build failed."

    $html = Get-Content -LiteralPath $output -Raw -Encoding UTF8
    $html = $html -replace '<body>', '<body><header><div class="topbar"><div class="brand">Hospital Operations Portal</div><div class="meta">โรงพยาบาลนาหมื่น | Version 1.0 | ปีงบประมาณ 2570</div></div></header><div class="layout">'
    $html = $html -replace '<nav id="TOC" role="doc-toc">', '<nav id="TOC" role="doc-toc" aria-label="สารบัญ">'
    if ($html -match '</nav>') {
        $html = $html -replace '</nav>', '</nav><main id="content">'
        $html = $html -replace '</body>', '</main></div></body>'
    } else {
        $html = $html -replace '<div class="layout">', '<div class="layout"><main id="content">'
        $html = $html -replace '</body>', '</main></div></body>'
    }
    [System.IO.File]::WriteAllText($output, $html, [System.Text.UTF8Encoding]::new($false))

    Write-Ok "HTML written: $output"
    exit 0
} catch {
    Write-Fail $_.Exception.Message
    Write-Warn "HTML build requires Pandoc. Output directory remains available for generated files."
    exit 1
}
