$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$verifier = Join-Path $repoRoot 'tools\verify-packages.ps1'
if (-not (Test-Path -LiteralPath $verifier)) {
    throw "Package verifier is missing: $verifier"
}

function Write-SourceManifest {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$Dll
    )
    $dir = Join-Path $Root "src/$Directory"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    @{
        id = $Id
        version = $Version
        dll = $Dll
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dir 'mod.json') -Encoding UTF8
}

function Add-ZipText {
    param($Zip, [string]$Name, [string]$Text)
    $entry = $Zip.CreateEntry($Name)
    $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
    try { $writer.Write($Text) } finally { $writer.Dispose() }
}

function New-Package {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][hashtable]$Manifest,
        [Parameter(Mandatory = $true)][string[]]$Entries
    )
    $zip = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipText -Zip $zip -Name 'mod.json' -Text ($Manifest | ConvertTo-Json -Compress)
        foreach ($entry in $Entries) {
            Add-ZipText -Zip $zip -Name $entry -Text 'fixture'
        }
    } finally {
        $zip.Dispose()
    }
}

function New-TestRoot {
    $root = Join-Path ([IO.Path]::GetTempPath()) ("swyf-packages-" + [Guid]::NewGuid().ToString('N'))
    $dist = Join-Path $root 'dist'
    New-Item -ItemType Directory -Force -Path $dist | Out-Null

    Write-SourceManifest -Root $root -Directory 'SpeechEnhanced' -Id 'CzechSpeechEnhanced' -Version '1.4.0' -Dll 'CzechSpeechEnhanced.dll'
    Write-SourceManifest -Root $root -Directory 'CzechTTSFix' -Id 'CzechTTSFix' -Version '1.0.0' -Dll 'CzechTTSFix.dll'
    Write-SourceManifest -Root $root -Directory 'CzechTranslationFix' -Id 'CzechTranslationFix' -Version '1.0.3' -Dll 'CzechTranslationFix.dll'

    New-Package -Path (Join-Path $dist 'CzechSpeechEnhanced-1.4.0.swyfmod') -Manifest @{ id='CzechSpeechEnhanced'; version='1.4.0'; dll='CzechSpeechEnhanced.dll' } -Entries @(
        'files/CzechSpeechEnhanced.dll',
        'files/Parakeet/bin/nemo-speech.dll',
        'files/licenses/NeMo-Speech-LICENSE.txt'
    )
    New-Package -Path (Join-Path $dist 'CzechTTSFix-1.0.0.swyfmod') -Manifest @{ id='CzechTTSFix'; version='1.0.0'; dll='CzechTTSFix.dll' } -Entries @(
        'files/CzechTTSFix.dll'
    )
    New-Package -Path (Join-Path $dist 'CzechTranslationFix-1.0.3.swyfmod') -Manifest @{ id='CzechTranslationFix'; version='1.0.3'; dll='CzechTranslationFix.dll' } -Entries @(
        'files/CzechTranslationFix.dll',
        'files/overrides.menu.json',
        'files/README.txt'
    )
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
    & $verifier -Root $valid -Dist (Join-Path $valid 'dist')

    $unexpected = New-TestRoot
    $roots += $unexpected
    $speechPath = Join-Path $unexpected 'dist/CzechSpeechEnhanced-1.4.0.swyfmod'
    $zip = [IO.Compression.ZipFile]::Open($speechPath, [IO.Compression.ZipArchiveMode]::Update)
    try { Add-ZipText -Zip $zip -Name 'files/unexpected.txt' -Text 'nope' } finally { $zip.Dispose() }
    Expect-Failure -Contains 'unexpected package entry' -Action { & $verifier -Root $unexpected -Dist (Join-Path $unexpected 'dist') }

    $metadataMismatch = New-TestRoot
    $roots += $metadataMismatch
    Remove-Item -LiteralPath (Join-Path $metadataMismatch 'dist/CzechTTSFix-1.0.0.swyfmod') -Force
    New-Package -Path (Join-Path $metadataMismatch 'dist/CzechTTSFix-1.0.0.swyfmod') -Manifest @{ id='CzechTTSFix'; version='9.9.9'; dll='CzechTTSFix.dll' } -Entries @('files/CzechTTSFix.dll')
    Expect-Failure -Contains 'embedded manifest' -Action { & $verifier -Root $metadataMismatch -Dist (Join-Path $metadataMismatch 'dist') }

    $privacyLeak = New-TestRoot
    $roots += $privacyLeak
    $speechPath = Join-Path $privacyLeak 'dist/CzechSpeechEnhanced-1.4.0.swyfmod'
    $zip = [IO.Compression.ZipFile]::Open($speechPath, [IO.Compression.ZipArchiveMode]::Update)
    try { Add-ZipText -Zip $zip -Name 'files/PrivacyTelemetry.dll' -Text 'nope' } finally { $zip.Dispose() }
    Expect-Failure -Contains 'PrivacyTelemetry' -Action { & $verifier -Root $privacyLeak -Dist (Join-Path $privacyLeak 'dist') }

    Write-Host 'PASS: public package content validation.'
} finally {
    foreach ($root in $roots) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}
