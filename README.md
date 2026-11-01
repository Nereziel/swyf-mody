# Scam With Your Friends mody (SWYF) – CZ/SK

Tři neoficiální mody pro **Scam With Your Friends** zaměřené na češtinu a slovenštinu.

**Web projektu:** https://nereziel.github.io/swyf-mody/

## Co mody dělají

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Nahrazuje herní Whisper při přepisu mikrofonu modelem NVIDIA Parakeet a zlepšuje rozpoznávání češtiny a slovenštiny. |
| **CzechTTSFix** | Opravuje zpracování českého textu pro Pocket TTS, aby se zachovala diakritika a text se správně připravil pro český hlas. |
| **CzechTranslationFix** | Doplňuje chybějící české texty a opravuje chybné, slepené nebo nepřirozené překlady v české lokalizaci hry. |

**Parakeet** je model pro převod mluvené řeči na text. Hra standardně používá Whisper; `CzechSpeechEnhanced` použije místo něj Parakeet TDT v3 nebo Parakeet Ultra.

> **Pozor:** `CzechTranslationFix` nepoužívej současně s modem **Lepší čeština**. Oba mody upravují českou lokalizaci.

## Instalace

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Na stránce [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest) stáhni požadovaný soubor `.swyfmod`.
3. Nainstaluj ho přes SWYF Mod Hub.
4. Restartuj hru.

U `CzechSpeechEnhanced` si v Mod Hubu zvolíš **Parakeet TDT v3** nebo **Parakeet Ultra**. Potřebný model se stáhne automaticky. Pauzu před dokončením věty lze nastavit na **1,3 až 3 sekundy**.

`CzechTranslationFix` používá překlady přibalené v souborech `overrides*.json`. Velký `translations.json` v repozitáři slouží pouze jako referenční export pro kontrolu lokalizace a za běhu se nenačítá.

## Lokální sestavení

Potřebuješ `.NET SDK` a nainstalovanou hru **Scam With Your Friends**.

```text
build.cmd
```

Běžné volby:

```text
All
Speech
TTS
Translation
```

`All` vytvoří v `dist/` všechny tři veřejné balíčky:

```text
CzechSpeechEnhanced-1.3.1.swyfmod
CzechTTSFix-1.0.0.swyfmod
CzechTranslationFix-1.0.2.swyfmod
```

Přímo přes PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

## GitHub Actions

Workflow **Build mods on self-hosted runner** se spouští pouze ručně přes `Actions → Run workflow`. Na self-hosted runneru sestaví všechny tři veřejné mody a uloží `.swyfmod` jako workflow artifact.

Workflow **Build and release mods** se také spouští pouze ručně. Zadáš společný tag releasu, například `v2026.11.01`, workflow sestaví všechny tři mody a vytvoří nebo aktualizuje jeden společný GitHub Release se třemi `.swyfmod` soubory. Verze jednotlivých modů se berou přímo z jejich `mod.json`.

Jeden společný release je záměr: stránka `releases/latest` tak vždy obsahuje všechny aktuální veřejné mody na jednom místě, i když mají každý vlastní verzi.

Lokální alternativa přes GitHub CLI zůstává dostupná přes `release.cmd`.

## CzechTranslationFix

Runtime překlady jsou rozdělené do `src/CzechTranslationFix/overrides*.json`. Mod je načítá z vlastní složky v `Mods/CzechTranslationFix`, aplikuje je pouze na české lokalizační tabulky a původní soubory hry nemění.

Podrobnosti jsou v [návodu k CzechTranslationFix](src/CzechTranslationFix/README.txt).

Test referenčního exportu a parseru:

```powershell
dotnet run --project tests/CzechTranslationFix -- src/CzechTranslationFix/translations.json
```

## Pokročilé

Experimentální `PrivacyTelemetry` není součástí běžného buildu `All` ani veřejného release. Ruční build:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods). Použité externí runtime a modely mají vlastní licence.
