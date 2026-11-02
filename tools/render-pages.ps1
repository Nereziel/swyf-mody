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
    @{ Directory='SpeechEnhanced'; Id='CzechSpeechEnhanced'; Placeholder='__SPEECH_VERSION__' },
    @{ Directory='CzechTTSFix'; Id='CzechTTSFix'; Placeholder='__TTS_VERSION__' },
    @{ Directory='CzechTranslationFix'; Id='CzechTranslationFix'; Placeholder='__TRANSLATION_VERSION__' }
)

$replacements = [ordered]@{
    '__GAME_VERSION__' = $gameVersion
}
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
    $replacements[$definition.Placeholder] = $version
}

$docsDir = Join-Path $Root 'docs'
$sourceIndex = Join-Path $docsDir 'index.html'
if (-not (Test-Path -LiteralPath $sourceIndex)) {
    throw "Website source is missing: $sourceIndex"
}

$sourceHtml = Get-Content -LiteralPath $sourceIndex -Raw -Encoding UTF8
foreach ($placeholder in $replacements.Keys) {
    if (-not $sourceHtml.Contains($placeholder)) {
        throw "Required website placeholder is missing: $placeholder"
    }
}

Remove-Item -LiteralPath $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath $docsDir -Destination $OutputDir -Recurse
Copy-Item -LiteralPath $gameVersionPath -Destination (Join-Path $OutputDir 'gameversion.txt')

$rendered = $sourceHtml
foreach ($placeholder in $replacements.Keys) {
    $rendered = $rendered.Replace($placeholder, $replacements[$placeholder])
}
if ($rendered -match '__[A-Z0-9_]+__') {
    throw "Website contains unresolved placeholder '$($Matches[0])'."
}

$outputIndex = Join-Path $OutputDir 'index.html'
[IO.File]::WriteAllText($outputIndex, $rendered, [Text.UTF8Encoding]::new($false))
Write-Host "Website metadata rendered: game=$gameVersion; Speech=$($replacements['__SPEECH_VERSION__']); TTS=$($replacements['__TTS_VERSION__']); Translation=$($replacements['__TRANSLATION_VERSION__'])"
