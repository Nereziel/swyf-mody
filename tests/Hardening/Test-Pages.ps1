$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$renderer = Join-Path $repoRoot 'tools\render-pages.ps1'
if (-not (Test-Path -LiteralPath $renderer)) {
    throw "Pages renderer is missing: $renderer"
}

function New-TestRoot {
    $root = Join-Path ([IO.Path]::GetTempPath()) ("swyf-pages-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path (Join-Path $root 'docs') | Out-Null
    Set-Content -LiteralPath (Join-Path $root 'gameversion.txt') -Value '0.1.95-swyf' -Encoding UTF8

    $mods = @(
        @{ Directory='SpeechEnhanced'; Id='CzechSpeechEnhanced'; Version='1.4.0'; Dll='CzechSpeechEnhanced.dll' },
        @{ Directory='CzechTTSFix'; Id='CzechTTSFix'; Version='1.0.0'; Dll='CzechTTSFix.dll' },
        @{ Directory='CzechTranslationFix'; Id='CzechTranslationFix'; Version='1.0.3'; Dll='CzechTranslationFix.dll' }
    )
    foreach ($mod in $mods) {
        $dir = Join-Path $root "src/$($mod.Directory)"
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
        @{ id=$mod.Id; version=$mod.Version; dll=$mod.Dll } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dir 'mod.json') -Encoding UTF8
    }

    @'
<html><body>
Game __GAME_VERSION__
Speech __SPEECH_VERSION__
TTS __TTS_VERSION__
Translation __TRANSLATION_VERSION__
</body></html>
'@ | Set-Content -LiteralPath (Join-Path $root 'docs/index.html') -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $root 'docs/.nojekyll') -Value '' -Encoding UTF8
    return $root
}

function Expect-Failure {
    param([scriptblock]$Action, [string]$Contains)
    try { & $Action } catch {
        if ($_.Exception.Message -notlike "*$Contains*") {
            throw "Expected failure containing '$Contains', got: $($_.Exception.Message)"
        }
        return
    }
    throw "Expected failure containing '$Contains', but action succeeded."
}

$roots = @()
try {
    $valid = New-TestRoot
    $roots += $valid
    $output = Join-Path $valid '_site'
    & $renderer -Root $valid -OutputDir $output
    $rendered = Get-Content -LiteralPath (Join-Path $output 'index.html') -Raw -Encoding UTF8
    foreach ($expected in @('0.1.95-swyf', '1.4.0', '1.0.0', '1.0.3')) {
        if (-not $rendered.Contains($expected)) { throw "Rendered website is missing $expected." }
    }
    if ($rendered -match '__[A-Z0-9_]+__') { throw 'Rendered website still contains a placeholder.' }
    $source = Get-Content -LiteralPath (Join-Path $valid 'docs/index.html') -Raw -Encoding UTF8
    if (-not $source.Contains('__GAME_VERSION__')) { throw 'Renderer modified source docs/index.html.' }

    $missing = New-TestRoot
    $roots += $missing
    $index = Join-Path $missing 'docs/index.html'
    (Get-Content -LiteralPath $index -Raw).Replace('__TTS_VERSION__', '1.0.0') | Set-Content -LiteralPath $index -Encoding UTF8
    Expect-Failure -Contains '__TTS_VERSION__' -Action { & $renderer -Root $missing -OutputDir (Join-Path $missing '_site') }

    Write-Host 'PASS: Pages metadata rendering.'
} finally {
    foreach ($root in $roots) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}
