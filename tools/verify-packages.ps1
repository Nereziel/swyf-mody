param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [string]$Dist = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$Root = (Resolve-Path -LiteralPath $Root).Path
$Dist = (Resolve-Path -LiteralPath $Dist).Path

function Read-ZipText {
    param($Entry)
    $reader = [IO.StreamReader]::new($Entry.Open(), [Text.Encoding]::UTF8, $true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function Test-SafeEntryName {
    param([Parameter(Mandatory = $true)][string]$Name)
    if ($Name.StartsWith('/') -or $Name.StartsWith('\')) { return $false }
    if ($Name.Contains('\')) { return $false }
    $segments = $Name.Split('/')
    return -not ($segments -contains '..')
}

$definitions = @(
    @{ Directory = 'SpeechEnhanced'; Id = 'CzechSpeechEnhanced' },
    @{ Directory = 'CzechTTSFix'; Id = 'CzechTTSFix' },
    @{ Directory = 'CzechTranslationFix'; Id = 'CzechTranslationFix' }
)

$sourceManifests = [ordered]@{}
foreach ($definition in $definitions) {
    $path = Join-Path $Root "src/$($definition.Directory)/mod.json"
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing source manifest: $path" }
    $manifest = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.id -ne $definition.Id) {
        throw "Source manifest id mismatch for $($definition.Id)."
    }
    $sourceManifests[$definition.Id] = $manifest
}

$packages = @(Get-ChildItem -LiteralPath $Dist -Filter '*.swyfmod' -File)
$privacyPackage = $packages | Where-Object Name -Like '*PrivacyTelemetry*'
if ($privacyPackage) {
    throw "PrivacyTelemetry must not be present in public packages: $($privacyPackage.Name)"
}
if ($packages.Count -ne $definitions.Count) {
    throw "Expected exactly $($definitions.Count) public .swyfmod packages, found $($packages.Count)."
}

foreach ($definition in $definitions) {
    $source = $sourceManifests[$definition.Id]
    $expectedName = "$($source.id)-$($source.version).swyfmod"
    $packagePath = Join-Path $Dist $expectedName
    if (-not (Test-Path -LiteralPath $packagePath)) {
        throw "Missing public package: $expectedName"
    }

    $zip = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $entries = @($zip.Entries)
        $names = @($entries | ForEach-Object FullName)

        foreach ($name in $names) {
            if (-not (Test-SafeEntryName -Name $name)) {
                throw "Unsafe package entry in ${expectedName}: $name"
            }
            if ($name -like '*PrivacyTelemetry*') {
                throw "PrivacyTelemetry content is forbidden in public package ${expectedName}: $name"
            }
        }

        $duplicates = @($names | Group-Object | Where-Object Count -gt 1)
        if ($duplicates.Count -gt 0) {
            throw "Duplicate package entry in ${expectedName}: $($duplicates[0].Name)"
        }

        $manifestEntries = @($entries | Where-Object FullName -eq 'mod.json')
        if ($manifestEntries.Count -ne 1) {
            throw "Package $expectedName must contain exactly one root mod.json."
        }
        $embedded = Read-ZipText -Entry $manifestEntries[0] | ConvertFrom-Json
        if ($embedded.id -ne $source.id -or $embedded.version -ne $source.version -or $embedded.dll -ne $source.dll) {
            throw "Package $expectedName embedded manifest does not match source manifest."
        }

        $dllEntry = "files/$($source.dll)"
        if ($names -notcontains $dllEntry) {
            throw "Package $expectedName is missing required DLL entry $dllEntry."
        }

        foreach ($name in $names) {
            $allowed = $false
            switch ($definition.Id) {
                'CzechSpeechEnhanced' {
                    $allowed = $name -eq 'mod.json' -or
                               $name -eq $dllEntry -or
                               $name.StartsWith('files/Parakeet/bin/') -or
                               $name.StartsWith('files/licenses/')
                }
                'CzechTTSFix' {
                    $allowed = $name -eq 'mod.json' -or $name -eq $dllEntry
                }
                'CzechTranslationFix' {
                    $allowed = $name -eq 'mod.json' -or
                               $name -eq $dllEntry -or
                               $name -eq 'files/README.txt' -or
                               ($name.StartsWith('files/overrides') -and $name.EndsWith('.json'))
                }
            }
            if (-not $allowed) {
                throw "Unexpected package entry in ${expectedName}: $name"
            }
        }

        if ($definition.Id -eq 'CzechSpeechEnhanced') {
            if (-not ($names | Where-Object { $_.StartsWith('files/Parakeet/bin/') })) {
                throw "Package $expectedName contains no Parakeet runtime files."
            }
            if (-not ($names | Where-Object { $_.StartsWith('files/licenses/') })) {
                throw "Package $expectedName contains no bundled runtime licenses."
            }
        }

        if ($definition.Id -eq 'CzechTranslationFix') {
            if ($names -notcontains 'files/README.txt') {
                throw "Package $expectedName is missing files/README.txt."
            }
            if (-not ($names | Where-Object { $_.StartsWith('files/overrides') -and $_.EndsWith('.json') })) {
                throw "Package $expectedName contains no translation override JSON files."
            }
        }
    } finally {
        $zip.Dispose()
    }

    Write-Host "Package OK: $expectedName"
}

Write-Host 'Public package set OK: 3 packages, no PrivacyTelemetry content.'
