$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$formatter = Join-Path $repoRoot 'tools\format-release-body.ps1'
if (-not (Test-Path -LiteralPath $formatter)) {
    throw "Release body formatter is missing: $formatter"
}

$existing = @'
**Ověřeno pro verzi hry:** `0.1.94-swyf`

## Balíčky

- stale

## Instalace

stale

> Tyto mody nejsou kompatibilní s modem **Lepší čeština**. Nepoužívej je současně.

**Full Changelog**: https://example.invalid/compare/old...new
'@

$body = & $formatter `
    -GameVersion '0.1.95-swyf' `
    -SpeechVersion '1.4.0' `
    -TtsVersion '1.0.0' `
    -TranslationVersion '1.0.3' `
    -ExistingBody $existing

foreach ($expected in @(
    '**Poslední ověřená verze hry:** `0.1.95-swyf`',
    '- **CzechSpeechEnhanced 1.4.0**',
    '- **CzechTTSFix 1.0.0**',
    '- **CzechTranslationFix 1.0.3**',
    '**Full Changelog**: https://example.invalid/compare/old...new'
)) {
    if (-not $body.Contains($expected)) { throw "Release body is missing: $expected" }
}
if ($body.Contains('- stale')) { throw 'Managed release body content was not replaced.' }
if (($body.Split('**Full Changelog**').Count - 1) -ne 1) { throw 'Release suffix was duplicated.' }

$second = & $formatter `
    -GameVersion '0.1.95-swyf' `
    -SpeechVersion '1.4.0' `
    -TtsVersion '1.0.0' `
    -TranslationVersion '1.0.3' `
    -ExistingBody $body
if ($second -ne $body) { throw 'Release body formatting is not idempotent.' }

$fresh = & $formatter `
    -GameVersion '0.1.95-swyf' `
    -SpeechVersion '1.4.0' `
    -TtsVersion '1.0.0' `
    -TranslationVersion '1.0.3'
if ($fresh.Contains('Full Changelog')) { throw 'Fresh managed body invented a changelog suffix.' }

Write-Host 'PASS: release body formatting and suffix preservation.'
