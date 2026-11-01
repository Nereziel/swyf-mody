param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [ValidateSet('All','Speech','TTS','Translation','Privacy')][string]$Mod = 'All',
    [string]$CacheDir
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($root)) {
    $root = Split-Path -Parent $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrWhiteSpace($CacheDir)) {
    $CacheDir = Join-Path $root '.cache'
}
$game = (Resolve-Path -LiteralPath $GameDir).Path
$managed = Join-Path $game 'Scam With Your Friends_Data\Managed'
if (-not (Test-Path -LiteralPath (Join-Path $managed 'ScriptsAssDef.dll'))) { throw 'Herní knihovny nebyly nalezeny.' }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $CacheDir, $dist | Out-Null
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

function Get-VerifiedFile([string]$Name, [string]$Url, [string]$Hash) {
    $path = Join-Path $CacheDir $Name
    if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Hash) {
        $ProgressPreference = 'SilentlyContinue'
        Write-Host "Stahuji: $Name"
        Invoke-WebRequest $Url -OutFile $path
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Hash) { throw "Nesouhlasí kontrolní součet: $Name" }
    return $path
}

function Add-File($zip, [string]$path, [string]$entry) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Chybí soubor: $path" }
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $path, $entry, [IO.Compression.CompressionLevel]::Optimal)
}

function Add-ArchiveEntry($zip, $entry, [string]$name) {
    $target = $zip.CreateEntry($name)
    $inputStream = $entry.Open(); $outputStream = $target.Open()
    try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose(); $inputStream.Dispose() }
}

function Build-Project([string]$projectDir) {
    $project = Get-ChildItem -LiteralPath $projectDir -Filter *.csproj | Select-Object -First 1
    if (-not $project) { throw "Projekt nebyl nalezen: $projectDir" }
    dotnet build $project.FullName -c Release -t:Rebuild "-p:GameDir=$game"
    if ($LASTEXITCODE -ne 0) { throw "Sestavení $($project.Name) selhalo." }
}

function Open-Package([string]$projectDir) {
    $manifestPath = Join-Path $projectDir 'mod.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $out = Join-Path $dist "$($manifest.id)-$($manifest.version).swyfmod"
    if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out }
    $zip = [IO.Compression.ZipFile]::Open($out, [IO.Compression.ZipArchiveMode]::Create)
    Add-File $zip $manifestPath 'mod.json'
    $bin = Join-Path $projectDir 'bin\Release'
    Add-File $zip (Join-Path $bin $manifest.dll) "files/$($manifest.dll)"
    $pdb = [IO.Path]::ChangeExtension((Join-Path $bin $manifest.dll), '.pdb')
    if (Test-Path -LiteralPath $pdb) { Add-File $zip $pdb "files/$([IO.Path]::GetFileName($pdb))" }
    return @{ Zip = $zip; Path = $out; Manifest = $manifest }
}

function Add-Licenses($zip) {
    $licenseDir = Join-Path $root 'licenses'
    if (Test-Path -LiteralPath $licenseDir) {
        foreach ($file in Get-ChildItem -LiteralPath $licenseDir -File) {
            Add-File $zip $file.FullName "files/licenses/$($file.Name)"
        }
    }
}

function Build-Speech {
    $dir = Join-Path $root 'src\SpeechEnhanced'
    Build-Project $dir
    $pkg = Open-Package $dir
    try {
        Add-Licenses $pkg.Zip
        $nemoPath = Get-VerifiedFile 'nemo-speech-0.3.0-windows-x86_64-vulkan.zip' 'https://github.com/NVIDIA/NeMo-Speech.cpp/releases/download/v0.3.0/nemo-speech-0.3.0-windows-x86_64-vulkan.zip' '72b3b8b41bfe2be0320eff4e9d310594e4f99deefcde4a157799a039656963ce'
        $runtime = [IO.Compression.ZipFile]::OpenRead($nemoPath)
        try {
            foreach ($entry in $runtime.Entries) {
                if ($entry.FullName -match '^nemo-speech-0\.3\.0-windows-x86_64-vulkan/bin/([^/]+)$') {
                    Add-ArchiveEntry $pkg.Zip $entry "files/Parakeet/bin/$($Matches[1])"
                }
            }
        } finally { $runtime.Dispose() }
    } finally { $pkg.Zip.Dispose() }
    Write-Output "Balíček: $($pkg.Path)"
}

function Build-Simple([string]$relativeDir) {
    $dir = Join-Path $root $relativeDir
    Build-Project $dir
    $pkg = Open-Package $dir
    $pkg.Zip.Dispose()
    Write-Output "Balíček: $($pkg.Path)"
}

function Build-Translation {
    $dir = Join-Path $root 'src\CzechTranslationFix'
    Build-Project $dir
    $pkg = Open-Package $dir
    try {
        Add-File $pkg.Zip (Join-Path $dir 'translations.json') 'files/translations.json'
        Add-File $pkg.Zip (Join-Path $dir 'README.txt') 'files/README.txt'
    } finally { $pkg.Zip.Dispose() }
    Write-Output "Balíček: $($pkg.Path)"
}

switch ($Mod) {
    'All' {
        # Public/user-facing packages only. PrivacyTelemetry stays available as an advanced explicit build.
        Build-Speech
        Build-Simple 'src\CzechTTSFix'
    }
    'Speech' { Build-Speech }
    'TTS' { Build-Simple 'src\CzechTTSFix' }
    'Translation' { Build-Translation }
    'Privacy' { Build-Simple 'src\PrivacyTelemetry' }
}
