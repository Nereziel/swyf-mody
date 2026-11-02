param(
    [Parameter(Mandatory = $true)][string]$GameVersion,
    [Parameter(Mandatory = $true)][string]$SpeechVersion,
    [Parameter(Mandatory = $true)][string]$TtsVersion,
    [Parameter(Mandatory = $true)][string]$TranslationVersion,
    [string]$ExistingBody = ''
)

$ErrorActionPreference = 'Stop'

if ($GameVersion -notmatch '^\d+\.\d+\.\d+-swyf$') {
    throw "Invalid game version '$GameVersion'."
}
foreach ($version in @($SpeechVersion, $TtsVersion, $TranslationVersion)) {
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Invalid mod version '$version'."
    }
}

$managedEnd = '> Tyto mody nejsou kompatibilní s modem **Lepší čeština**. Nepoužívej je současně.'
$body = @(
    "**Poslední ověřená verze hry:** ``$GameVersion``"
    ''
    '## Balíčky'
    ''
    "- **CzechSpeechEnhanced $SpeechVersion** – lepší CZ/SK přepis řeči přes Parakeet"
    "- **CzechTTSFix $TtsVersion** – oprava českého Pocket TTS"
    "- **CzechTranslationFix $TranslationVersion** – oprava české lokalizace"
    ''
    '## Instalace'
    ''
    'Stáhni požadovaný `.swyfmod` a nainstaluj ho přes SWYF Mod Hub.'
    ''
    $managedEnd
) -join "`n"

if (-not [string]::IsNullOrWhiteSpace($ExistingBody)) {
    $existingLines = @($ExistingBody -split "`r?`n")
    $managedEndIndex = -1
    for ($i = 0; $i -lt $existingLines.Count; $i++) {
        if ($existingLines[$i] -eq $managedEnd) {
            $managedEndIndex = $i
            break
        }
    }
    if ($managedEndIndex -ge 0 -and ($managedEndIndex + 1) -lt $existingLines.Count) {
        $suffix = ($existingLines[($managedEndIndex + 1)..($existingLines.Count - 1)] -join "`n").Trim()
        if (-not [string]::IsNullOrWhiteSpace($suffix)) {
            $body = "$body`n`n$suffix"
        }
    }
}

Write-Output $body
