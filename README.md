# Scam With Your Friends mody (SWYF) – čeština, slovenština a TTS

Neoficiální **mody pro Scam With Your Friends (SWYF)** zaměřené na české a slovenské hráče. Repo obsahuje lepší **rozpoznávání české a slovenské řeči** pomocí Parakeet a opravu českého **TTS**. Mody používají **BepInEx** a instalují se jako balíčky `.swyfmod`.

**Web projektu:** https://nereziel.github.io/swyf-mody/

## Nejdřív nainstaluj SWYF Mod Hub

Pro instalaci je potřeba **SWYF Mod Hub od Dexe**:

**https://dexhauls.github.io/swyf-mods/**

Mod Hub nainstaluje potřebný **BepInEx** a umí instalovat balíčky `.swyfmod`.

## Dostupné mody

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Lepší rozpoznávání češtiny a slovenštiny pomocí NVIDIA Parakeet TDT v3 nebo Parakeet Ultra. |
| **CzechTTSFix** | Opravuje zpracování českého textu pro herní TTS, aby NPC správně používala diakritiku a interpunkci. |

Speech mod čeká přibližně **1,3 sekundy ticha** před ukončením věty, aby krátká přirozená pauza při mluvení nerozdělila jednu větu na dvě.

## Instalace

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Stáhni požadovaný `.swyfmod` z [GitHub Releases](https://github.com/Nereziel/swyf-mody/releases).
3. Nainstaluj ho přes SWYF Mod Hub.
4. U `CzechSpeechEnhanced` případně vyber Parakeet nebo Parakeet Ultra a způsob výpočtu.
5. Restartuj hru.

## Lokální sestavení

Potřebuješ `.NET SDK`, nainstalovanou hru **Scam With Your Friends** a BepInEx.

Nejjednodušší je spustit:

```text
build.cmd
```

Běžné volby jsou:

```text
All
Speech
TTS
```

`All` sestaví oba veřejné mody. Výsledné balíčky vzniknou v `dist/` jako `.swyfmod`.

Přímo přes PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

## Vytvoření GitHub Release

Pro automatický release je potřeba [GitHub CLI](https://cli.github.com/). Poprvé se přihlas:

```text
gh auth login
```

Potom například:

```text
release.cmd 1.3.1
```

`release.cmd` sestaví a nahraje pouze dva veřejné balíčky: `CzechSpeechEnhanced` a `CzechTTSFix`.

## Pokročilé

V repozitáři zůstává také experimentální `PrivacyTelemetry`, ale není součástí běžného buildu ani veřejného release. Pokud ho chceš sestavit ručně:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods) a MIT licence tohoto repozitáře nepřelicencovává cizí nebo odvozený kód. Použité externí runtime/modely mají vlastní licence.
