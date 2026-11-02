param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [string]$ReleaseTag
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path -LiteralPath $Root).Path

function Get-PluginVersion {
    param([Parameter(Mandatory = $true)][string]$Path)

    $source = Get-Content -LiteralPath $Path -Raw -Encoding UTF8

    $constant = [regex]::Match(
        $source,
        '\bPluginVersion\s*=\s*"(?<version>[^"\r\n]+)"'
    )
    if ($constant.Success) {
        return $constant.Groups['version'].Value
    }

    $attribute = [regex]::Match(
        $source,
        'BepInPlugin\s*\(\s*"[^"]+"\s*,\s*"[^"]+"\s*,\s*"(?<version>[^"\r\n]+)"\s*\)'
    )
    if ($attribute.Success) {
        return $attribute.Groups['version'].Value
    }

    throw "Could not determine plugin version from $Path."
}

$gameVersionPath = Join-Path $Root 'gameversion.txt'
if (-not (Test-Path -LiteralPath $gameVersionPath)) {
    throw 'gameversion.txt is missing.'
}

$gameVersionLines = @(Get-Content -LiteralPath $gameVersionPath -Encoding UTF8)
if ($gameVersionLines.Count -ne 1) {
    throw 'gameversion.txt must contain exactly one line.'
}
$gameVersion = $gameVersionLines[0].Trim()
if ($gameVersion -notmatch '^\d+\.\d+\.\d+-swyf$') {
    throw "gameversion.txt must match X.Y.Z-swyf, got '$gameVersion'."
}

$mods = @(
    @{ Directory = 'SpeechEnhanced'; Id = 'CzechSpeechEnhanced' },
    @{ Directory = 'CzechTTSFix'; Id = 'CzechTTSFix' },
    @{ Directory = 'CzechTranslationFix'; Id = 'CzechTranslationFix' }
)

$versions = [ordered]@{}
foreach ($mod in $mods) {
    $dir = Join-Path $Root "src/$($mod.Directory)"
    $manifestPath = Join-Path $dir 'mod.json'
    $pluginPath = Join-Path $dir 'Plugin.cs'

    if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Missing manifest: $manifestPath" }
    if (-not (Test-Path -LiteralPath $pluginPath)) { throw "Missing plugin source: $pluginPath" }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.id -ne $mod.Id) {
        throw "Manifest id mismatch in $manifestPath: expected '$($mod.Id)', got '$($manifest.id)'."
    }
    if ([string]$manifest.version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Manifest version for $($mod.Id) must match X.Y.Z, got '$($manifest.version)'."
    }
    if ([string]::IsNullOrWhiteSpace([string]$manifest.dll)) {
        throw "Manifest dll is missing for $($mod.Id)."
    }

    $pluginVersion = Get-PluginVersion -Path $pluginPath
    if ($pluginVersion -notmatch '^\d+\.\d+\.\d+$') {
        throw "Plugin version for $($mod.Id) must match X.Y.Z, got '$pluginVersion'."
    }
    if ($pluginVersion -ne [string]$manifest.version) {
        throw "Plugin/manifest version mismatch for $($mod.Id): plugin=$pluginVersion manifest=$($manifest.version)."
    }

    $versions[$mod.Id] = [string]$manifest.version
}

if (-not [string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $match = [regex]::Match(
        $ReleaseTag.Trim(),
        '^v(?<date>\d{4}\.\d{2}\.\d{2})-(?<game>\d+\.\d+\.\d+)$'
    )
    if (-not $match.Success) {
        throw "Invalid release tag '$ReleaseTag'; expected vYYYY.MM.DD-X.Y.Z."
    }

    try {
        [void][DateTime]::ParseExact(
            $match.Groups['date'].Value,
            'yyyy.MM.dd',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::None
        )
    } catch {
        throw "Invalid release tag '$ReleaseTag'; date is not valid."
    }

    $tagGameVersion = "$($match.Groups['game'].Value)-swyf"
    if ($tagGameVersion -ne $gameVersion) {
        throw "Release tag game version '$tagGameVersion' does not match gameversion.txt '$gameVersion'."
    }
}

Write-Host "Metadata OK: game=$gameVersion; Speech=$($versions.CzechSpeechEnhanced); TTS=$($versions.CzechTTSFix); Translation=$($versions.CzechTranslationFix)"
