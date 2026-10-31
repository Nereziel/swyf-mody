param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [ValidateSet('All','Speech','Canary','TTS','Privacy')][string]$Mod = 'All',
    [string]$CacheDir = (Join-Path $PSScriptRoot '.cache')
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$game = (Resolve-Path -LiteralPath $GameDir).Path
$managed = Join-Path $game 'Scam With Your Friends_Data\Managed'
if (-not (Test-Path -LiteralPath (Join-Path $managed 'ScriptsAssDef.dll'))) { throw 'Herní knihovny nebyly nalezeny.' }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $CacheDir, $dist | Out-Null
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

function Get-VerifiedArchive([string]$Name, [string]$Url, [string]$Hash) {
    $path = Join-Path $CacheDir $Name
    if (-not (Test-Path -LiteralPath $path) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $Hash) {
        $ProgressPreference = 'SilentlyContinue'
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
        foreach ($file in Get-ChildItem -LiteralPath $licenseDir -File) { Add-File $zip $file.FullName "files/licenses/$($file.Name)" }
    }
}

function Build-Speech {
    $dir = Join-Path $root 'src\SpeechEnhanced'
    Build-Project $dir
    $pkg = Open-Package $dir
    try {
        Add-Licenses $pkg.Zip
        $nemoPath = Get-VerifiedArchive 'nemo-speech-0.3.0-windows-x86_64-vulkan.zip' 'https://github.com/NVIDIA/NeMo-Speech.cpp/releases/download/v0.3.0/nemo-speech-0.3.0-windows-x86_64-vulkan.zip' '72b3b8b41bfe2be0320eff4e9d310594e4f99deefcde4a157799a039656963ce'
        $runtime = [IO.Compression.ZipFile]::OpenRead($nemoPath)
        try {
            foreach ($entry in $runtime.Entries) {
                if ($entry.FullName -match '^nemo-speech-0\.3\.0-windows-x86_64-vulkan/bin/([^/]+)$') { Add-ArchiveEntry $pkg.Zip $entry "files/Parakeet/bin/$($Matches[1])" }
            }
        } finally { $runtime.Dispose() }

        $pkPath = Get-VerifiedArchive 'parakeet-v0.6.1-bin-win-vulkan-x64.zip' 'https://github.com/mudler/parakeet.cpp/releases/download/v0.6.1/parakeet-v0.6.1-bin-win-vulkan-x64.zip' '1b9adfb041394ebd016865a15699614a3c095876e07e7c3d67669d5a00cdda86'
        $runtime = [IO.Compression.ZipFile]::OpenRead($pkPath)
        try {
            $entry = $runtime.Entries | Where-Object { $_.FullName -match '(^|/)parakeet-server\.exe$' } | Select-Object -First 1
            if (-not $entry) { throw 'Nemotron server nebyl nalezen.' }
            Add-ArchiveEntry $pkg.Zip $entry 'files/Nemotron/bin/parakeet-server.exe'
        } finally { $runtime.Dispose() }
    } finally { $pkg.Zip.Dispose() }
    Write-Output "Balíček: $($pkg.Path)"
}

function Build-Canary {
    $dir = Join-Path $root 'src\CzechSpeechEnhancedCanary'
    Build-Project $dir
    $pkg = Open-Package $dir
    try {
        Add-Licenses $pkg.Zip
        $python = Get-VerifiedArchive 'python-3.14.9-embed-amd64.zip' 'https://www.python.org/ftp/python/3.14.9/python-3.14.9-embed-amd64.zip' '3c01d7cfc666f746a6f064f60fb2156ab0ec464d3d738ab796371a46a9bb24a2'
        $portable = Join-Path $CacheDir 'canary-python-gpu'
        $ready = Join-Path $portable 'canary-deps-v2-gpu.ready'
        if (-not (Test-Path -LiteralPath $ready)) {
            if (Test-Path -LiteralPath $portable) { Remove-Item -LiteralPath $portable -Recurse -Force }
            New-Item -ItemType Directory -Force -Path $portable | Out-Null
            Expand-Archive -LiteralPath $python -DestinationPath $portable -Force
            Add-Content -LiteralPath (Join-Path $portable 'python314._pth') -Value 'Lib\site-packages'
            py -3.14 -m pip install --target (Join-Path $portable 'Lib\site-packages') -r (Join-Path $root 'src\canary-requirements.txt')
            if ($LASTEXITCODE -ne 0) { throw 'Instalace závislostí Canary selhala. Nainstaluj Python 3.14 a pip.' }
            & (Join-Path $portable 'python.exe') -c 'import onnx_asr, onnxruntime, numpy'
            if ($LASTEXITCODE -ne 0) { throw 'Ověření Canary runtime selhalo.' }
            [IO.File]::WriteAllText($ready, 'onnx-asr 0.10.1; onnxruntime-gpu 1.31.1; CUDA 13')
        }
        Add-File $pkg.Zip (Join-Path $root 'src\canary_server.py') 'files/Canary/canary_server.py'
        foreach ($file in Get-ChildItem -LiteralPath $portable -Recurse -File) {
            $relative = $file.FullName.Substring($portable.Length + 1).Replace('\', '/')
            if ($relative -match '^canary-deps-.*\.ready$|(^|/)__pycache__/|\.pyc$') { continue }
            Add-File $pkg.Zip $file.FullName "files/Canary/python/$relative"
        }
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

switch ($Mod) {
    'All' {
        Build-Speech
        Build-Canary
        Build-Simple 'src\CzechTTSFix'
        Build-Simple 'src\PrivacyTelemetry'
    }
    'Speech' { Build-Speech }
    'Canary' { Build-Canary }
    'TTS' { Build-Simple 'src\CzechTTSFix' }
    'Privacy' { Build-Simple 'src\PrivacyTelemetry' }
}
