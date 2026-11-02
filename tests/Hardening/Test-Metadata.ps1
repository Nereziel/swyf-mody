$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$validator = Join-Path $repoRoot 'tools\validate-release-metadata.ps1'
if (-not (Test-Path -LiteralPath $validator)) {
    throw "Metadata validator is missing: $validator"
}

function Write-TestPlugin {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$Dll,
        [switch]$UseConstant
    )

    $dir = Join-Path $Root "src/$Directory"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    @{
        id = $Id
        version = $Version
        dll = $Dll
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dir 'mod.json') -Encoding UTF8

    if ($UseConstant) {
        @"
using BepInEx;
[BepInPlugin("test.$Id", "$Id", PluginVersion)]
public class Plugin {
    public const string PluginVersion = "$Version";
}
"@ | Set-Content -LiteralPath (Join-Path $dir 'Plugin.cs') -Encoding UTF8
    } else {
        @"
using BepInEx;
[BepInPlugin("test.$Id", "$Id", "$Version")]
public class Plugin { }
"@ | Set-Content -LiteralPath (Join-Path $dir 'Plugin.cs') -Encoding UTF8
    }
}

function New-TestRoot {
    param([string]$GameVersion = '0.1.95-swyf')

    $root = Join-Path ([IO.Path]::GetTempPath()) ("swyf-metadata-" + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    Set-Content -LiteralPath (Join-Path $root 'gameversion.txt') -Value $GameVersion -Encoding UTF8
    Write-TestPlugin -Root $root -Directory 'SpeechEnhanced' -Id 'CzechSpeechEnhanced' -Version '1.4.0' -Dll 'CzechSpeechEnhanced.dll' -UseConstant
    Write-TestPlugin -Root $root -Directory 'CzechTTSFix' -Id 'CzechTTSFix' -Version '1.0.0' -Dll 'CzechTTSFix.dll'
    Write-TestPlugin -Root $root -Directory 'CzechTranslationFix' -Id 'CzechTranslationFix' -Version '1.0.3' -Dll 'CzechTranslationFix.dll'
    return $root
}

function Expect-Failure {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$Contains
    )

    try {
        & $Action
    } catch {
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
    & $validator -Root $valid -ReleaseTag 'v2026.11.02-0.1.95'

    $versionMismatch = New-TestRoot
    $roots += $versionMismatch
    $manifestPath = Join-Path $versionMismatch 'src/SpeechEnhanced/mod.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $manifest.version = '1.4.1'
    $manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    Expect-Failure -Contains 'version mismatch' -Action { & $validator -Root $versionMismatch }

    $invalidGame = New-TestRoot -GameVersion '0.1.95'
    $roots += $invalidGame
    Expect-Failure -Contains 'gameversion.txt' -Action { & $validator -Root $invalidGame }

    $multiLine = New-TestRoot
    $roots += $multiLine
    Set-Content -LiteralPath (Join-Path $multiLine 'gameversion.txt') -Value @('0.1.95-swyf', 'unexpected') -Encoding UTF8
    Expect-Failure -Contains 'exactly one' -Action { & $validator -Root $multiLine }

    $badTag = New-TestRoot
    $roots += $badTag
    Expect-Failure -Contains 'release tag' -Action { & $validator -Root $badTag -ReleaseTag 'release-0.1.95' }

    $badDate = New-TestRoot
    $roots += $badDate
    Expect-Failure -Contains 'release tag' -Action { & $validator -Root $badDate -ReleaseTag 'v2026.13.40-0.1.95' }

    $tagMismatch = New-TestRoot
    $roots += $tagMismatch
    Expect-Failure -Contains 'does not match' -Action { & $validator -Root $tagMismatch -ReleaseTag 'v2026.11.02-0.1.94' }

    Write-Host 'PASS: release metadata validation.'
} finally {
    foreach ($root in $roots) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}
