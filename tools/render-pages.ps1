param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $Root '_site'
} elseif (-not [IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = Join-Path $Root $OutputDir
}
$OutputDir = [IO.Path]::GetFullPath($OutputDir)

$gameVersionPath = Join-Path $Root 'gameversion.txt'
$gameVersionLines = @(Get-Content -LiteralPath $gameVersionPath -Encoding UTF8)
if ($gameVersionLines.Count -ne 1) {
    throw 'gameversion.txt must contain exactly one line.'
}
$gameVersion = $gameVersionLines[0].Trim()
if ($gameVersion -notmatch '^\d+\.\d+\.\d+-swyf$') {
    throw "gameversion.txt must match X.Y.Z-swyf, got '$gameVersion'."
}

$manifestDefinitions = @(
    @{ Directory='SpeechEnhanced'; Id='CzechSpeechEnhanced' },
    @{ Directory='CzechTTSFix'; Id='CzechTTSFix' },
    @{ Directory='CzechTranslationFix'; Id='CzechTranslationFix' }
)

$versions = [ordered]@{}
foreach ($definition in $manifestDefinitions) {
    $manifestPath = Join-Path $Root "src/$($definition.Directory)/mod.json"
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.id -ne $definition.Id) {
        throw "Manifest id mismatch in ${manifestPath}: expected '$($definition.Id)', got '$($manifest.id)'."
    }
    $version = [string]$manifest.version
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Manifest version for $($definition.Id) must match X.Y.Z, got '$version'."
    }
    $versions[$definition.Id] = $version
}

$docsDir = Join-Path $Root 'docs'
$sourceIndex = Join-Path $docsDir 'index.html'
if (-not (Test-Path -LiteralPath $sourceIndex)) {
    throw "Website source is missing: $sourceIndex"
}

$sourceHtml = Get-Content -LiteralPath $sourceIndex -Raw -Encoding UTF8
if (-not $sourceHtml.Contains('__GAME_VERSION__')) {
    throw 'Required website placeholder is missing: __GAME_VERSION__'
}

$rendered = $sourceHtml.Replace('__GAME_VERSION__', $gameVersion)
foreach ($definition in $manifestDefinitions) {
    $id = $definition.Id
    $sourceMarker = "<span class=`"tech`">$id</span>"
    if (-not $rendered.Contains($sourceMarker)) {
        throw "Required website mod marker is missing: $id"
    }
    $renderedMarker = "<span class=`"tech`">$id · v$($versions[$id])</span>"
    $rendered = $rendered.Replace($sourceMarker, $renderedMarker)
}

if ($rendered -match '__[A-Z0-9_]+__') {
    throw "Website contains unresolved placeholder '$($Matches[0])'."
}

Remove-Item -LiteralPath $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath $docsDir -Destination $OutputDir -Recurse
Copy-Item -LiteralPath $gameVersionPath -Destination (Join-Path $OutputDir 'gameversion.txt')

$outputIndex = Join-Path $OutputDir 'index.html'
[IO.File]::WriteAllText($outputIndex, $rendered, [Text.UTF8Encoding]::new($false))
Write-Host "Website metadata rendered: game=$gameVersion; Speech=$($versions.CzechSpeechEnhanced); TTS=$($versions.CzechTTSFix); Translation=$($versions.CzechTranslationFix)"
