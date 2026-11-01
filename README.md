# Scam With Your Friends Mods (SWYF) – Czech/Slovak Speech, TTS Fix & Privacy

Neoficiální **Scam With Your Friends mods / SWYF mods** pro Windows: lepší české a slovenské rozpoznávání řeči přes **Parakeet**, oprava českého **TTS** a omezení herní telemetrie. Mody používají **BepInEx** a instalují se jako `.swyfmod` balíčky.

Unofficial **Scam With Your Friends mods** for Czech and Slovak players: Parakeet speech recognition, Czech TTS fixes and privacy/telemetry controls.

## Nejdřív nainstaluj SWYF Mod Hub

Pro tyto mody je potřeba **SWYF Mod Hub od Dexe**:

**https://dexhauls.github.io/swyf-mods/**

Mod Hub nainstaluje potřebný **BepInEx** a umí instalovat balíčky `.swyfmod`.

## Scam With Your Friends mody v tomto repozitáři

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Lepší rozpoznávání češtiny a slovenštiny pomocí NVIDIA Parakeet TDT v3 nebo Parakeet Ultra. |
| **CzechTTSFix** | Opravuje zpracování českého textu pro herní TTS, NPC už mluví s diakritikou. |
| **PrivacyTelemetry** | Blokuje známou analytiku hry a omezuje diagnostická data odesílaná ve feedbacku. |

Speech mod čeká přibližně **1,3 sekundy ticha** před ukončením věty, aby krátká pauza při mluvení nerozdělila jednu větu na dvě.

## Instalace SWYF modů

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Stáhni požadovaný `.swyfmod` z [GitHub Releases](https://github.com/Nereziel/swyf-mody/releases).
3. Nainstaluj ho přes SWYF Mod Hub.
4. U speech modu případně vyber model a způsob výpočtu.
5. Restartuj hru.

## Lokální sestavení

Potřebuješ `.NET SDK`, nainstalovanou hru **Scam With Your Friends** a BepInEx.

Nejjednodušší je spustit v kořeni repozitáře:

```text
build.cmd
```

Skript se zeptá na cestu ke hře a co chceš sestavit:

```text
All
Speech
TTS
Privacy
```

Výsledné balíčky vzniknou v `dist/` jako `.swyfmod`.

Lze použít i přímo PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

## Vytvoření GitHub Release

Pro automatický release je potřeba [GitHub CLI](https://cli.github.com/). Poprvé se přihlas:

```text
gh auth login
```

Potom stačí například:

```text
release.cmd 1.3.1
```

`release.cmd` ověří, že lokální repo odpovídá `origin/main`, smaže staré `.swyfmod` z `dist/`, sestaví všechny 3 mody, vytvoří tag/release `v1.3.1`, vygeneruje release notes a nahraje nové balíčky. Cestu ke hře si vyžádá při spuštění.

Pokud release se stejným tagem už existuje, skript nabídne nahrazení jeho `.swyfmod` souborů.

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods) a MIT licence tohoto repozitáře nepřelicencovává cizí nebo odvozený kód. Použité externí runtime/modely mají vlastní licence.
