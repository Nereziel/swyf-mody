param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [ValidateSet('All','Speech','TTS','Privacy')][string]$Mod = 'All',
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

function Add-File($zip, [string]$path, [string]$entry, [IO.Compression.CompressionLevel]$compression = [IO.Compression.CompressionLevel]::Optimal) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Chybí soubor: $path" }
    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $path, $entry, $compression)
}

function Add-ArchiveEntry($zip, $entry, [string]$name) {
    $target = $zip.CreateEntry($name)
    $inputStream = $entry.Open(); $outputStream = $target.Open()
    try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose(); $inputStream.Dispose() }
}

function Add-TextEntry($zip, [string]$text, [string]$entry) {
    $target = $zip.CreateEntry($entry)
    $stream = $target.Open()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($text)
        $stream.Write($bytes, 0, $bytes.Length)
    } finally {
        $stream.Dispose()
    }
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
        foreach ($file in Get-ChildItem -LiteralPath $licenseDir -File) { Add-File $zip $file.FullName "files/licenses/$($file.Name)" }
    }
}

function New-ManualPackage([string]$packagePath, $manifest, [string]$extraFile = '', [string]$extraRelativePath = '', [string]$extraNote = '') {
    $out = Join-Path $dist "$($manifest.id)-$($manifest.version).zip"
    if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out }

    $source = [IO.Compression.ZipFile]::OpenRead($packagePath)
    $target = [IO.Compression.ZipFile]::Open($out, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $source.Entries) {
            if ($entry.Name.Length -eq 0) { continue }
            $name = $entry.FullName.Replace('\', '/')
            if ($name -eq 'mod.json') {
                $manualName = "$($manifest.id)/mod.json"
            } elseif ($name.StartsWith('files/')) {
                $manualName = "$($manifest.id)/" + $name.Substring(6)
            } else {
                continue
            }
            Add-ArchiveEntry $target $entry $manualName
        }

        if (-not [string]::IsNullOrWhiteSpace($extraFile)) {
            # GGUF model is already compact; storing it without another deflate pass makes ZIP creation much faster.
            Add-File $target $extraFile "$($manifest.id)/$extraRelativePath" ([IO.Compression.CompressionLevel]::NoCompression)
        }

        $note = @"
RUČNÍ INSTALACE - $($manifest.name)
========================================

1. Hra už musí mít nainstalovaný základ pro mody (BepInEx + SWYF Mod Manager).
   Nejjednodušší je jednou nainstalovat SWYF Mod Hub.

2. Rozbal tento ZIP.

3. Zkopíruj CELOU složku:
   $($manifest.id)

   do složky hry:
   Mods

4. Výsledná cesta musí vypadat takto:
   ...\Scam With Your Friends\Mods\$($manifest.id)\mod.json

5. Restartuj hru.

$extraNote
Pokud chceš jednodušší instalaci a automatické aktualizace, použij místo ZIPu balíček .swyfmod přes SWYF Mod Hub.
"@
        Add-TextEntry $target $note "$($manifest.id)/INSTALACE.txt"
    } finally {
        $target.Dispose()
        $source.Dispose()
    }

    Write-Host "Ruční ZIP: $out"
    return $out
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

    $defaultModel = $pkg.Manifest.downloads | Where-Object { $_.default -eq $true } | Select-Object -First 1
    if (-not $defaultModel) { throw 'V manifestu CzechSpeechEnhanced chybí výchozí Parakeet model.' }
    $modelName = [IO.Path]::GetFileName([string]$defaultModel.path)
    $modelPath = Get-VerifiedFile $modelName ([string]$defaultModel.url) ([string]$defaultModel.sha256)
    $manualNote = "Tento ZIP už obsahuje výchozí model Parakeet TDT v3, takže po zkopírování složky do Mods není potřeba model dál stahovat.`r`nParakeet Ultra v ručním ZIPu není; pro jeho snadnou instalaci použij .swyfmod přes SWYF Mod Hub.`r`n"
    [void](New-ManualPackage $pkg.Path $pkg.Manifest $modelPath 'Parakeet/parakeet.q8.gguf' $manualNote)

    Write-Output "Balíček: $($pkg.Path)"
}

function Build-Simple([string]$relativeDir) {
    $dir = Join-Path $root $relativeDir
    Build-Project $dir
    $pkg = Open-Package $dir
    $pkg.Zip.Dispose()
    [void](New-ManualPackage $pkg.Path $pkg.Manifest)
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
    'Privacy' { Build-Simple 'src\PrivacyTelemetry' }
}
